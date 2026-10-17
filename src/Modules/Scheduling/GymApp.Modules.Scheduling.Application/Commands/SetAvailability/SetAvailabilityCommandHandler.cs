using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.SetAvailability;

internal sealed class SetAvailabilityCommandHandler : ICommandHandler<SetAvailabilityCommand>
{
    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public SetAvailabilityCommandHandler(
        ITrainerAvailabilityRepository availabilityRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _availabilityRepository = availabilityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var existing = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        var availability = existing ?? TrainerAvailability.Create(
            request.TrainerId,
            request.GymId,
            request.SessionDurationMinutes,
            request.BufferMinutes);

        if (existing is not null)
        {
            availability.UpdateSessionDuration(request.SessionDurationMinutes, request.BufferMinutes);

            // Remove existing slots and re-add
            RemoveAllSlots(availability);
        }

        var addResult = AddSlots(availability, request.WeeklySlots);
        if (addResult.IsFailure)
            return addResult;

        if (existing is null)
            await _availabilityRepository.AddAsync(availability, cancellationToken);
        else
            _availabilityRepository.Update(availability);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void RemoveAllSlots(TrainerAvailability availability)
    {
        var existingSlotIds = availability.WeeklySlots.Select(s => s.Id).ToList();
        foreach (var slotId in existingSlotIds)
            availability.RemoveSlot(slotId);
    }

    // Stops at the first slot that overlaps one already added.
    private static Result AddSlots(TrainerAvailability availability, IEnumerable<AvailabilitySlotDto> slots)
    {
        foreach (var slot in slots)
        {
            var addResult = availability.AddSlot(slot.DayOfWeek, slot.StartTime, slot.EndTime);
            if (addResult.IsFailure)
                return addResult;
        }

        return Result.Success();
    }
}

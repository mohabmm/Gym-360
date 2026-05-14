using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
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
        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (availability is null)
        {
            availability = TrainerAvailability.Create(
                request.TrainerId,
                request.GymId,
                request.SessionDurationMinutes,
                request.BufferMinutes);

            foreach (var slot in request.WeeklySlots)
            {
                var addResult = availability.AddSlot(slot.DayOfWeek, slot.StartTime, slot.EndTime);
                if (addResult.IsFailure)
                    return addResult;
            }

            await _availabilityRepository.AddAsync(availability, cancellationToken);
        }
        else
        {
            availability.UpdateSessionDuration(request.SessionDurationMinutes, request.BufferMinutes);

            // Remove existing slots and re-add
            var existingSlotIds = availability.WeeklySlots.Select(s => s.Id).ToList();
            foreach (var slotId in existingSlotIds)
            {
                availability.RemoveSlot(slotId);
            }

            foreach (var slot in request.WeeklySlots)
            {
                var addResult = availability.AddSlot(slot.DayOfWeek, slot.StartTime, slot.EndTime);
                if (addResult.IsFailure)
                    return addResult;
            }

            _availabilityRepository.Update(availability);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

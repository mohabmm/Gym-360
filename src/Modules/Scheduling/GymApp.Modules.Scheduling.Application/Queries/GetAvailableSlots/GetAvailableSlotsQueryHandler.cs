using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetAvailableSlots;

internal sealed class GetAvailableSlotsQueryHandler
    : IQueryHandler<GetAvailableSlotsQuery, List<TimeSlotDto>>
{
    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly ISessionRepository _sessionRepository;

    public GetAvailableSlotsQueryHandler(
        ITrainerAvailabilityRepository availabilityRepository,
        ISessionRepository sessionRepository)
    {
        _availabilityRepository = availabilityRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<List<TimeSlotDto>>> Handle(
        GetAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        // No availability, or the date is blocked: no slots
        if (availability is null || availability.IsDateBlocked(request.Date))
            return Result.Success(new List<TimeSlotDto>());

        var grid = SlotGrid.For(availability);
        var slots = grid.SlotsIn(availability.ActiveSlotsOn(request.Date.DayOfWeek)).ToList();

        if (slots.Count == 0)
            return Result.Success(new List<TimeSlotDto>());

        var bookedSessions = (await _sessionRepository.GetByDateAndTrainerAsync(
                request.TrainerId, request.Date, cancellationToken))
            .Where(SlotGrid.HoldsSlot)
            .ToList();

        var timeSlots = slots
            .Select(slot => new TimeSlotDto(
                TimeOnly.FromTimeSpan(slot.Start),
                TimeOnly.FromTimeSpan(slot.End),
                !bookedSessions.Any(s => grid.Overlaps(s, slot))))
            .ToList();

        return Result.Success(timeSlots);
    }
}

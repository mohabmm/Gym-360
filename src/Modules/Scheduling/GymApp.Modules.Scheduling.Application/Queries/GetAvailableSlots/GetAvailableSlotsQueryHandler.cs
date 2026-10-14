using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Enums;
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

        if (availability is null)
            return Result.Success(new List<TimeSlotDto>());

        // If the date is blocked, no slots available
        if (availability.IsDateBlocked(request.Date))
            return Result.Success(new List<TimeSlotDto>());

        var dayOfWeek = request.Date.DayOfWeek;
        var activeSlots = availability.WeeklySlots
            .Where(s => s.IsActive && s.DayOfWeek == dayOfWeek)
            .ToList();

        if (!activeSlots.Any())
            return Result.Success(new List<TimeSlotDto>());

        // Get booked sessions for this date
        var bookedSessions = await _sessionRepository.GetByDateAndTrainerAsync(
            request.TrainerId, request.Date, cancellationToken);

        var activeSessions = bookedSessions
            .Where(s => s.Status is not (SessionStatus.CancelledByTrainer
                or SessionStatus.CancelledByMember
                or SessionStatus.LateCancelled
                or SessionStatus.NoShow))
            .ToList();

        var timeSlots = new List<TimeSlotDto>();

        // Guard: a non-positive duration would never advance the cursor below.
        if (availability.SessionDurationMinutes <= 0)
            return Result.Success(timeSlots);

        var sessionDuration = TimeSpan.FromMinutes(availability.SessionDurationMinutes);
        var buffer = TimeSpan.FromMinutes(Math.Max(0, availability.BufferMinutes));

        // Generate time slots from availability windows.
        // Arithmetic is done on TimeSpan, NOT TimeOnly: TimeOnly.AddMinutes wraps past
        // midnight, so a window ending late in the evening (e.g. 23:59) made
        // "start + duration <= end" true forever — the request never returned and the
        // slot list grew until the process ran out of memory.
        foreach (var slot in activeSlots)
        {
            var cursor = slot.StartTime.ToTimeSpan();
            var windowEnd = slot.EndTime.ToTimeSpan();

            while (cursor + sessionDuration <= windowEnd)
            {
                var slotStart = cursor;
                var slotEnd = cursor + sessionDuration;

                // Check if this slot conflicts with any booked session (including buffer)
                var isBooked = activeSessions.Any(s =>
                {
                    var sessionStart = s.ScheduledTime.ToTimeSpan();
                    var sessionEnd = sessionStart + TimeSpan.FromMinutes(s.DurationMinutes) + buffer;
                    return slotStart < sessionEnd && sessionStart < slotEnd + buffer;
                });

                timeSlots.Add(new TimeSlotDto(
                    TimeOnly.FromTimeSpan(slotStart),
                    TimeOnly.FromTimeSpan(slotEnd),
                    !isBooked));

                // Move to next slot (session duration + buffer)
                cursor += sessionDuration + buffer;
            }
        }

        return Result.Success(timeSlots);
    }
}

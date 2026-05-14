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

        var sessionDuration = availability.SessionDurationMinutes;
        var buffer = availability.BufferMinutes;
        var timeSlots = new List<TimeSlotDto>();

        // Generate time slots from availability windows
        foreach (var slot in activeSlots)
        {
            var currentTime = slot.StartTime;
            var slotEnd = slot.EndTime;

            while (currentTime.AddMinutes(sessionDuration) <= slotEnd)
            {
                var endTime = currentTime.AddMinutes(sessionDuration);

                // Check if this slot conflicts with any booked session (including buffer)
                var isBooked = activeSessions.Any(s =>
                {
                    var sessionStart = s.ScheduledTime;
                    var sessionEnd = s.ScheduledTime.AddMinutes(s.DurationMinutes + buffer);
                    return currentTime < sessionEnd && sessionStart < endTime.AddMinutes(buffer);
                });

                timeSlots.Add(new TimeSlotDto(currentTime, endTime, !isBooked));

                // Move to next slot (session duration + buffer)
                currentTime = currentTime.AddMinutes(sessionDuration + buffer);
            }
        }

        return Result.Success(timeSlots);
    }
}

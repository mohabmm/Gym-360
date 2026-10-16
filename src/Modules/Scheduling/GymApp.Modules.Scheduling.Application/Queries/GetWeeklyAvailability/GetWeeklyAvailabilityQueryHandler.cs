using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetWeeklyAvailability;

internal sealed class GetWeeklyAvailabilityQueryHandler
    : IQueryHandler<GetWeeklyAvailabilityQuery, WeeklyAvailabilityDto>
{
    private const string StatusAvailable = "Available";
    private const string StatusBookedByMe = "BookedByMe";
    private const string StatusBookedByOther = "BookedByOther";

    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly ISessionRepository _sessionRepository;

    public GetWeeklyAvailabilityQueryHandler(
        ITrainerAvailabilityRepository availabilityRepository,
        ISessionRepository sessionRepository)
    {
        _availabilityRepository = availabilityRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<WeeklyAvailabilityDto>> Handle(
        GetWeeklyAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result.Failure<WeeklyAvailabilityDto>(SchedulingErrors.InvalidDateRange);

        if ((request.EndDate.DayNumber - request.StartDate.DayNumber) > 30)
            return Result.Failure<WeeklyAvailabilityDto>(SchedulingErrors.InvalidDateRange);

        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        var sessions = await _sessionRepository.GetByTrainerAsync(
            request.TrainerId, request.GymId, request.StartDate, request.EndDate, cancellationToken);

        var activeSessions = sessions
            .Where(s => s.Status is not (SessionStatus.CancelledByTrainer
                or SessionStatus.CancelledByMember
                or SessionStatus.LateCancelled
                or SessionStatus.NoShow))
            .ToList();

        var sessionDuration = availability?.SessionDurationMinutes ?? 60;
        var buffer = availability?.BufferMinutes ?? 15;
        // Arithmetic is done on TimeSpan, NOT TimeOnly: TimeOnly.AddMinutes wraps past
        // midnight, so a window ending late in the evening (e.g. 23:59) made
        // "start + duration <= end" true forever and the request never returned.
        var slotRules = new SlotRules(
            request.RequestingMemberId,
            TimeSpan.FromMinutes(sessionDuration),
            TimeSpan.FromMinutes(Math.Max(0, buffer)));
        var days = new List<DayAvailabilityDto>();

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            days.Add(BuildDay(date, availability, activeSessions, slotRules));
        }

        return Result.Success(new WeeklyAvailabilityDto(
            request.TrainerId,
            request.GymId,
            request.StartDate,
            request.EndDate,
            sessionDuration,
            buffer,
            days));
    }

    private sealed record SlotRules(Guid? RequestingMemberId, TimeSpan Duration, TimeSpan Buffer);

    private static DayAvailabilityDto BuildDay(
        DateOnly date,
        TrainerAvailability? availability,
        List<Session> activeSessions,
        SlotRules rules)
    {
        if (availability is null)
        {
            return new DayAvailabilityDto(date, date.DayOfWeek, IsOffDay: true, IsBlocked: false, null, new List<WeekSlotDto>());
        }

        var blocked = availability.BlockedDates.FirstOrDefault(b => b.Date == date);
        if (blocked is not null)
        {
            return new DayAvailabilityDto(date, date.DayOfWeek, IsOffDay: false, IsBlocked: true, blocked.Reason, new List<WeekSlotDto>());
        }

        var weeklySlotsForDay = availability.WeeklySlots
            .Where(s => s.IsActive && s.DayOfWeek == date.DayOfWeek)
            .ToList();

        if (!weeklySlotsForDay.Any())
        {
            return new DayAvailabilityDto(date, date.DayOfWeek, IsOffDay: true, IsBlocked: false, null, new List<WeekSlotDto>());
        }

        var sessionsForDay = activeSessions.Where(s => s.ScheduledDate == date).ToList();

        // A non-positive duration would never advance the cursor.
        var slots = rules.Duration <= TimeSpan.Zero
            ? new List<WeekSlotDto>()
            : weeklySlotsForDay.SelectMany(w => BuildWindowSlots(w, sessionsForDay, rules)).ToList();

        return new DayAvailabilityDto(date, date.DayOfWeek, IsOffDay: false, IsBlocked: false, null, slots);
    }

    private static IEnumerable<WeekSlotDto> BuildWindowSlots(
        AvailabilitySlot window, List<Session> sessionsForDay, SlotRules rules)
    {
        var cursor = window.StartTime.ToTimeSpan();
        var windowEnd = window.EndTime.ToTimeSpan();

        while (cursor + rules.Duration <= windowEnd)
        {
            var slotStart = cursor;
            var slotEnd = cursor + rules.Duration;

            var conflicting = sessionsForDay.FirstOrDefault(s => Overlaps(s, slotStart, slotEnd, rules.Buffer));

            yield return new WeekSlotDto(
                TimeOnly.FromTimeSpan(slotStart),
                TimeOnly.FromTimeSpan(slotEnd),
                StatusFor(conflicting, rules.RequestingMemberId),
                conflicting?.Id);

            cursor += rules.Duration + rules.Buffer;
        }
    }

    private static bool Overlaps(Session session, TimeSpan slotStart, TimeSpan slotEnd, TimeSpan buffer)
    {
        var sStart = session.ScheduledTime.ToTimeSpan();
        var sEnd = sStart + TimeSpan.FromMinutes(session.DurationMinutes) + buffer;
        return slotStart < sEnd && sStart < slotEnd + buffer;
    }

    private static string StatusFor(Session? conflicting, Guid? requestingMemberId)
    {
        if (conflicting is null)
            return StatusAvailable;

        return conflicting.MemberId == requestingMemberId ? StatusBookedByMe : StatusBookedByOther;
    }
}

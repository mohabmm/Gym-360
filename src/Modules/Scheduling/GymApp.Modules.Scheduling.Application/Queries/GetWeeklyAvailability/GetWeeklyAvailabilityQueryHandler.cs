using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

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
            request.TrainerId, new GymDateRange(request.GymId, request.StartDate, request.EndDate), cancellationToken);

        var activeSessions = sessions.Where(SlotGrid.HoldsSlot).ToList();

        var sessionDuration = availability?.SessionDurationMinutes ?? 60;
        var buffer = availability?.BufferMinutes ?? 15;
        var days = new List<DayAvailabilityDto>();

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            days.Add(BuildDay(date, availability, activeSessions, request.RequestingMemberId));
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

    private static DayAvailabilityDto BuildDay(
        DateOnly date,
        TrainerAvailability? availability,
        List<Session> activeSessions,
        Guid? requestingMemberId)
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

        var windows = availability.ActiveSlotsOn(date.DayOfWeek);
        if (windows.Count == 0)
        {
            return new DayAvailabilityDto(date, date.DayOfWeek, IsOffDay: true, IsBlocked: false, null, new List<WeekSlotDto>());
        }

        var grid = SlotGrid.For(availability);
        var sessionsForDay = activeSessions.Where(s => s.ScheduledDate == date).ToList();
        var slots = grid.SlotsIn(windows)
            .Select(slot => ToWeekSlot(slot, sessionsForDay.FirstOrDefault(s => grid.Overlaps(s, slot)), requestingMemberId))
            .ToList();

        return new DayAvailabilityDto(date, date.DayOfWeek, IsOffDay: false, IsBlocked: false, null, slots);
    }

    private static WeekSlotDto ToWeekSlot((TimeSpan Start, TimeSpan End) slot, Session? conflicting, Guid? requestingMemberId)
    {
        var start = TimeOnly.FromTimeSpan(slot.Start);
        var end = TimeOnly.FromTimeSpan(slot.End);

        if (conflicting is null)
            return new WeekSlotDto(start, end, StatusAvailable, null);

        var status = conflicting.MemberId == requestingMemberId ? StatusBookedByMe : StatusBookedByOther;
        return new WeekSlotDto(start, end, status, conflicting.Id);
    }
}

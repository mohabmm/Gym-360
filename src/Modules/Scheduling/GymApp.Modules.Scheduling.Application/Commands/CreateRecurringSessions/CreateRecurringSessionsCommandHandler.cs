using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.CreateRecurringSessions;

internal sealed class CreateRecurringSessionsCommandHandler
    : ICommandHandler<CreateRecurringSessionsCommand, List<Guid>>
{
    private const int MaxWeeksAhead = 12;

    private readonly ISessionRepository _sessionRepository;
    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public CreateRecurringSessionsCommandHandler(
        ISessionRepository sessionRepository,
        ITrainerAvailabilityRepository availabilityRepository,
        IDateTimeProvider dateTimeProvider,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _availabilityRepository = availabilityRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<Guid>>> Handle(
        CreateRecurringSessionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SessionType>(request.SessionType, true, out var sessionType))
            return Result.Failure<List<Guid>>(new Error(
                "Scheduling.InvalidSessionType", "The provided session type is not valid."));

        var weeksAhead = Math.Min(request.WeeksAhead, MaxWeeksAhead);

        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (availability is null)
            return Result.Failure<List<Guid>>(SchedulingErrors.AvailabilityNotFoundForTrainer(request.TrainerId));

        if (!availability.HasAvailableSlot(request.DayOfWeek, request.Time))
            return Result.Failure<List<Guid>>(SchedulingErrors.SlotNotAvailable);

        var startDate = NextOccurrenceAfter(GymLocalTime.Today(_dateTimeProvider.UtcNow), request.DayOfWeek);
        var occurrenceDates = Enumerable.Range(0, Math.Max(weeksAhead, 0))
            .Select(i => startDate.AddDays(i * 7))
            .Where(date => !availability.IsDateBlocked(date));

        var sessionIds = await ScheduleOccurrencesAsync(
            request,
            new RecurringBooking(sessionType, weeksAhead, availability.BufferMinutes),
            occurrenceDates,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sessionIds);
    }

    // Books every free occurrence; dates that clash with an existing session are skipped.
    private async Task<List<Guid>> ScheduleOccurrencesAsync(
        CreateRecurringSessionsCommand request,
        RecurringBooking booking,
        IEnumerable<DateOnly> occurrenceDates,
        CancellationToken cancellationToken)
    {
        var recurrenceRule = $"WEEKLY;DAY={request.DayOfWeek};WEEKS={booking.WeeksAhead}";
        var window = new BookingWindow(request.Time, request.DurationMinutes, booking.BufferMinutes);

        var sessionIds = new List<Guid>();
        foreach (var sessionDate in occurrenceDates)
        {
            // Check for conflicts on this date
            var existingSessions = await _sessionRepository.GetByDateAndTrainerAsync(
                request.TrainerId, sessionDate, cancellationToken);

            if (window.ConflictsWithAny(existingSessions))
                continue;

            var session = Session.CreateRecurring(
                new SessionParticipants(request.GymId, request.TrainerId, request.MemberId),
                new SessionSchedule(sessionDate, request.Time, request.DurationMinutes),
                booking.SessionType,
                recurrenceRule);

            await _sessionRepository.AddAsync(session, cancellationToken);
            sessionIds.Add(session.Id);
        }

        return sessionIds;
    }

    // Next occurrence of the requested day of week; start from next week if it is today.
    private static DateOnly NextOccurrenceAfter(DateOnly today, DayOfWeek dayOfWeek)
    {
        var daysUntilTarget = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0)
            daysUntilTarget = 7;

        return today.AddDays(daysUntilTarget);
    }
}

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

        var sessionIds = new List<Guid>();
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        // Find the next occurrence of the requested day of week
        var daysUntilTarget = ((int)request.DayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0)
            daysUntilTarget = 7; // Start from next week if today

        var startDate = today.AddDays(daysUntilTarget);
        var recurrenceRule = $"WEEKLY;DAY={request.DayOfWeek};WEEKS={weeksAhead}";

        for (var i = 0; i < weeksAhead; i++)
        {
            var sessionDate = startDate.AddDays(i * 7);

            if (availability.IsDateBlocked(sessionDate))
                continue;

            // Check for conflicts on this date
            var existingSessions = await _sessionRepository.GetByDateAndTrainerAsync(
                request.TrainerId, sessionDate, cancellationToken);

            var hasConflict = false;
            var requestEnd = request.Time.AddMinutes(request.DurationMinutes);
            var bufferMinutes = availability.BufferMinutes;

            foreach (var existing in existingSessions)
            {
                if (existing.Status is SessionStatus.CancelledByTrainer
                    or SessionStatus.CancelledByMember
                    or SessionStatus.LateCancelled
                    or SessionStatus.NoShow)
                    continue;

                var existingEnd = existing.ScheduledTime.AddMinutes(existing.DurationMinutes + bufferMinutes);
                var newEnd = requestEnd.AddMinutes(bufferMinutes);

                if (request.Time < existingEnd && existing.ScheduledTime < newEnd)
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
                continue;

            var session = Session.CreateRecurring(
                new SessionParticipants(request.GymId, request.TrainerId, request.MemberId),
                new SessionSchedule(sessionDate, request.Time, request.DurationMinutes),
                sessionType,
                recurrenceRule);

            await _sessionRepository.AddAsync(session, cancellationToken);
            sessionIds.Add(session.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sessionIds);
    }
}

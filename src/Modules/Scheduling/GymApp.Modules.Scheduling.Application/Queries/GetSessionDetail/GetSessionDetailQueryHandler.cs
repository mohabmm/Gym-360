using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionDetail;

internal sealed class GetSessionDetailQueryHandler
    : IQueryHandler<GetSessionDetailQuery, SessionDetailDto>
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionDetailQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<SessionDetailDto>> Handle(
        GetSessionDetailQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure<SessionDetailDto>(
                SchedulingErrors.SessionNotFound(request.SessionId));

        var detail = new SessionDetailDto(
            session.Id,
            session.GymId,
            session.TrainerId,
            session.MemberId,
            session.ScheduledDate.ToDateTime(session.ScheduledTime, DateTimeKind.Utc),
            session.DurationMinutes,
            session.SessionType.ToString(),
            session.Focus,
            session.Status.ToString(),
            session.IsRecurring,
            session.RecurrenceRule,
            session.CancellationReason,
            session.CancelledAt,
            session.CreatedAt,
            ToDto(session.TrainerLog),
            ToDto(session.MemberLog),
            session.ExerciseLogs.OrderBy(l => l.OrderIndex).Select(ToDto).ToList());

        return Result.Success(detail);
    }

    private static TrainerLogDto? ToDto(TrainerLog? log) =>
        log is null
            ? null
            : new TrainerLogDto(
                log.Id,
                log.ExercisesPerformed,
                log.ActualVsPlanned,
                log.ClientEffortRating,
                log.Feedback,
                log.TechniqueNotes,
                log.AreasToImprove,
                log.NextSessionFocus,
                log.AttendanceConfirmed,
                log.LoggedAt);

    private static MemberLogDto? ToDto(MemberLog? log) =>
        log is null
            ? null
            : new MemberLogDto(
                log.Id,
                log.Rating,
                log.PerceivedExertion,
                log.PersonalNotes,
                log.Discomfort,
                log.Satisfaction,
                log.LoggedAt);

    private static SessionExerciseLogDto ToDto(SessionExerciseLog log) =>
        new(
            log.Id,
            log.SessionId,
            log.ExerciseId,
            log.ExerciseName,
            log.ExerciseNameAr,
            log.OrderIndex,
            log.Sets,
            log.Reps,
            log.Weight,
            log.Notes,
            log.CreatedAt,
            log.UpdatedAt);
}

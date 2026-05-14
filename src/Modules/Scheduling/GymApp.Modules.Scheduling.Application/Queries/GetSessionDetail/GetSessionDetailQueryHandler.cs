using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
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

        TrainerLogDto? trainerLog = null;
        if (session.TrainerLog is not null)
        {
            trainerLog = new TrainerLogDto(
                session.TrainerLog.Id,
                session.TrainerLog.ExercisesPerformed,
                session.TrainerLog.ActualVsPlanned,
                session.TrainerLog.ClientEffortRating,
                session.TrainerLog.Feedback,
                session.TrainerLog.TechniqueNotes,
                session.TrainerLog.AreasToImprove,
                session.TrainerLog.NextSessionFocus,
                session.TrainerLog.AttendanceConfirmed,
                session.TrainerLog.LoggedAt);
        }

        MemberLogDto? memberLog = null;
        if (session.MemberLog is not null)
        {
            memberLog = new MemberLogDto(
                session.MemberLog.Id,
                session.MemberLog.Rating,
                session.MemberLog.PerceivedExertion,
                session.MemberLog.PersonalNotes,
                session.MemberLog.Discomfort,
                session.MemberLog.Satisfaction,
                session.MemberLog.LoggedAt);
        }

        var exerciseLogs = session.ExerciseLogs
            .OrderBy(l => l.OrderIndex)
            .Select(l => new SessionExerciseLogDto(
                l.Id,
                l.SessionId,
                l.ExerciseId,
                l.ExerciseName,
                l.ExerciseNameAr,
                l.OrderIndex,
                l.Sets,
                l.Reps,
                l.Weight,
                l.Notes,
                l.CreatedAt,
                l.UpdatedAt))
            .ToList();

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
            trainerLog,
            memberLog,
            exerciseLogs);

        return Result.Success(detail);
    }
}

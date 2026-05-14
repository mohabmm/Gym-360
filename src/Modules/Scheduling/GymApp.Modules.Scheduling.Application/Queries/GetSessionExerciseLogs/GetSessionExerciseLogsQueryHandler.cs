using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionExerciseLogs;

internal sealed class GetSessionExerciseLogsQueryHandler
    : IQueryHandler<GetSessionExerciseLogsQuery, IReadOnlyList<SessionExerciseLogDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionExerciseLogsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<IReadOnlyList<SessionExerciseLogDto>>> Handle(
        GetSessionExerciseLogsQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(
                SchedulingErrors.SessionNotFound(request.SessionId));

        if (session.TrainerId != request.CallerId && session.MemberId != request.CallerId)
            return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(SchedulingErrors.NotSessionParticipant);

        IReadOnlyList<SessionExerciseLogDto> logs = session.ExerciseLogs
            .OrderBy(l => l.OrderIndex)
            .Select(l => new SessionExerciseLogDto(
                l.Id, l.SessionId, l.ExerciseId, l.ExerciseName, l.ExerciseNameAr,
                l.OrderIndex, l.Sets, l.Reps, l.Weight, l.Notes, l.CreatedAt, l.UpdatedAt))
            .ToList();

        return Result.Success(logs);
    }
}

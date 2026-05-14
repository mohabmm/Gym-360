using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetWorkoutLogs;

internal sealed class GetWorkoutLogsQueryHandler : IQueryHandler<GetWorkoutLogsQuery, List<WorkoutLogDto>>
{
    private readonly IWorkoutLogRepository _workoutLogRepository;

    public GetWorkoutLogsQueryHandler(IWorkoutLogRepository workoutLogRepository)
    {
        _workoutLogRepository = workoutLogRepository;
    }

    public async Task<Result<List<WorkoutLogDto>>> Handle(
        GetWorkoutLogsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var logs = await _workoutLogRepository.GetByUserAsync(
            request.UserId, from, to, cancellationToken);

        var dtos = logs.Select(log => new WorkoutLogDto(
            log.Id,
            log.UserId,
            log.GymId,
            log.WorkoutId,
            log.Date,
            log.StartedAt,
            log.CompletedAt,
            log.DurationMinutes,
            log.Notes,
            log.LogSets.Select(s => new LogSetDto(
                s.ExerciseId,
                s.SetNumber,
                s.Reps,
                s.Weight,
                s.WeightUnit.ToString(),
                s.DurationSeconds,
                s.IsCompleted,
                s.Notes)).ToList(),
            log.MuscleGroupsWorked.Select(mg => mg.ToString()).ToList(),
            log.PerceivedExertion)).ToList();

        return Result.Success(dtos);
    }
}

using GymApp.Modules.Training.Application.Contracts;
using GymApp.Modules.Training.Application.DTOs;
using System.Linq.Expressions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Services;

internal sealed class TrainingQueryService : ITrainingQueryService
{
    private readonly TrainingDbContext _dbContext;

    public TrainingQueryService(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetWorkoutCountForTrainerAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Workouts
            .AsNoTracking()
            .CountAsync(w => w.TrainerId == trainerId && w.GymId == gymId, cancellationToken);
    }

    public async Task<List<WorkoutLogDto>> GetRecentWorkoutLogsAsync(
        Guid userId,
        Guid gymId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.WorkoutLogs
            .AsNoTracking()
            .Include(wl => wl.LogSets.OrderBy(ls => ls.SetNumber))
            .Where(wl => wl.UserId == userId && wl.GymId == gymId)
            .OrderByDescending(wl => wl.Date)
            .Take(count)
            .ToListAsync(cancellationToken);

        return logs.Select(wl => new WorkoutLogDto(
            wl.Id,
            wl.UserId,
            wl.GymId,
            wl.WorkoutId,
            wl.Date,
            wl.StartedAt,
            wl.CompletedAt,
            wl.DurationMinutes,
            wl.Notes,
            wl.LogSets.Select(ls => new LogSetDto(
                ls.ExerciseId,
                ls.SetNumber,
                ls.Reps,
                ls.Weight,
                ls.WeightUnit.ToString(),
                ls.DurationSeconds,
                ls.IsCompleted,
                ls.Notes)).ToList(),
            wl.MuscleGroupsWorked.Select(mg => mg.ToString()).ToList(),
            wl.PerceivedExertion)).ToList();
    }

    public async Task<TodaysWorkoutResponse?> GetTodaysAssignmentAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var assignment = await _dbContext.WorkoutAssignments
            .AsNoTracking()
            .Where(wa => wa.ClientId == userId && wa.GymId == gymId)
            .Where(IsActiveWorkoutAssignmentOn(today))
            .OrderByDescending(wa => wa.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment?.WorkoutId is not { } workoutId)
            return null;

        var workout = await _dbContext.Workouts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workoutId, cancellationToken);

        return workout is null ? null : ToTodaysWorkoutResponse(assignment, workout);
    }

    public async Task<bool> IsRemoteWorkoutActiveAsync(
        Guid clientId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RemoteWorkoutConfigs
            .AsNoTracking()
            .AnyAsync(rwc =>
                rwc.ClientId == clientId &&
                rwc.GymId == gymId &&
                rwc.IsActive,
                cancellationToken);
    }

    private static Expression<Func<WorkoutAssignment, bool>> IsActiveWorkoutAssignmentOn(DateOnly today) =>
        wa => wa.Status == AssignmentStatus.Active &&
              wa.StartDate <= today &&
              (wa.EndDate == null || wa.EndDate >= today) &&
              wa.WorkoutId != null;

    private static TodaysWorkoutResponse ToTodaysWorkoutResponse(WorkoutAssignment assignment, Workout workout)
    {
        return new TodaysWorkoutResponse(
            assignment.Id,
            workout.Id,
            workout.Name,
            workout.Description,
            workout.EstimatedDurationMinutes,
            workout.TargetMuscleGroups.Select(mg => mg.ToString()).ToList(),
            assignment.IsRemoteMode,
            assignment.StartDate,
            assignment.EndDate);
    }
}

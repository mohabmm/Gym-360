using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetTodaysWorkout;

internal sealed class GetTodaysWorkoutQueryHandler : IQueryHandler<GetTodaysWorkoutQuery, TodaysWorkoutResponse?>
{
    private readonly IWorkoutAssignmentRepository _assignmentRepository;
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IRemoteWorkoutConfigRepository _remoteWorkoutConfigRepository;

    public GetTodaysWorkoutQueryHandler(
        IWorkoutAssignmentRepository assignmentRepository,
        IWorkoutRepository workoutRepository,
        IRemoteWorkoutConfigRepository remoteWorkoutConfigRepository)
    {
        _assignmentRepository = assignmentRepository;
        _workoutRepository = workoutRepository;
        _remoteWorkoutConfigRepository = remoteWorkoutConfigRepository;
    }

    public async Task<Result<TodaysWorkoutResponse?>> Handle(
        GetTodaysWorkoutQuery request, CancellationToken cancellationToken)
    {
        // Check if remote workout is active — if not, return null
        var remoteConfig = await _remoteWorkoutConfigRepository.GetActiveByClientAsync(
            request.UserId, request.GymId, cancellationToken);

        var assignments = await _assignmentRepository.GetByClientAsync(
            request.UserId, request.GymId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeAssignment = assignments.FirstOrDefault(a => IsActiveWorkoutAssignment(a, today));

        if (activeAssignment?.WorkoutId is not { } workoutId)
        {
            return Result.Success<TodaysWorkoutResponse?>(null);
        }

        var workout = await _workoutRepository.GetByIdAsync(workoutId, cancellationToken);

        if (workout is null)
        {
            return Result.Success<TodaysWorkoutResponse?>(null);
        }

        var isRemoteMode = remoteConfig?.IsActive == true || activeAssignment.IsRemoteMode;

        return Result.Success<TodaysWorkoutResponse?>(ToResponse(activeAssignment, workout, isRemoteMode));
    }

    private static bool IsActiveWorkoutAssignment(WorkoutAssignment assignment, DateOnly today) =>
        assignment.Status == AssignmentStatus.Active
        && CoversDate(assignment, today)
        && assignment.WorkoutId.HasValue;

    private static bool CoversDate(WorkoutAssignment assignment, DateOnly date) =>
        assignment.StartDate <= date && (assignment.EndDate is null || assignment.EndDate >= date);

    private static TodaysWorkoutResponse ToResponse(
        WorkoutAssignment assignment, Workout workout, bool isRemoteMode) =>
        new(
            assignment.Id,
            workout.Id,
            workout.Name,
            workout.Description,
            workout.EstimatedDurationMinutes,
            workout.TargetMuscleGroups.Select(mg => mg.ToString()).ToList(),
            isRemoteMode,
            assignment.StartDate,
            assignment.EndDate);
}

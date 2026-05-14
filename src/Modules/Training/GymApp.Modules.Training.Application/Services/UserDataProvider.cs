using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly IWorkoutAssignmentRepository _workoutAssignmentRepository;

    public UserDataProvider(
        IWorkoutLogRepository workoutLogRepository,
        IWorkoutAssignmentRepository workoutAssignmentRepository)
    {
        _workoutLogRepository = workoutLogRepository;
        _workoutAssignmentRepository = workoutAssignmentRepository;
    }

    public string ModuleName => "Training";

    public async Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var recentLogs = await _workoutLogRepository.GetByUserAsync(
            userId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        return new
        {
            WorkoutLogs = recentLogs.Select(l => new
            {
                l.Id,
                l.Date,
                l.WorkoutId,
                l.GymId,
                l.DurationMinutes,
                l.PerceivedExertion,
                l.Notes,
                SetCount = l.LogSets.Count
            })
        };
    }
}

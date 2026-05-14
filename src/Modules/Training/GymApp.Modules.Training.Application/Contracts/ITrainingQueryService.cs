using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Contracts;

public interface ITrainingQueryService
{
    Task<int> GetWorkoutCountForTrainerAsync(Guid trainerId, Guid gymId, CancellationToken cancellationToken = default);
    Task<List<WorkoutLogDto>> GetRecentWorkoutLogsAsync(Guid userId, Guid gymId, int count, CancellationToken cancellationToken = default);
    Task<TodaysWorkoutResponse?> GetTodaysAssignmentAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);
    Task<bool> IsRemoteWorkoutActiveAsync(Guid clientId, Guid gymId, CancellationToken cancellationToken = default);
}

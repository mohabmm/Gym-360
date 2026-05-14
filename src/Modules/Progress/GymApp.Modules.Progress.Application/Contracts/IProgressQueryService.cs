using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Contracts;

public interface IProgressQueryService
{
    Task<BodyMetricDto?> GetLatestMetricsAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);
    Task<decimal?> GetCurrentWeightAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetProgressPhotoCountAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetWorkoutStreakAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);
}

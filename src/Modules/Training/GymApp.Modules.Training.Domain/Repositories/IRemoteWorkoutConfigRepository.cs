using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IRemoteWorkoutConfigRepository
{
    Task<RemoteWorkoutConfig?> GetByTrainerAndClientAsync(Guid trainerId, Guid clientId, Guid gymId, CancellationToken cancellationToken = default);
    Task<RemoteWorkoutConfig?> GetActiveByClientAsync(Guid clientId, Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(RemoteWorkoutConfig config, CancellationToken cancellationToken = default);
    void Update(RemoteWorkoutConfig config);
}

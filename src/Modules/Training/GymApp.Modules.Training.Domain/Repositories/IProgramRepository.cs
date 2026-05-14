using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IProgramRepository
{
    Task<Program?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Program>> GetByTrainerAndGymAsync(Guid trainerId, Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(Program program, CancellationToken cancellationToken = default);
    void Update(Program program);
}

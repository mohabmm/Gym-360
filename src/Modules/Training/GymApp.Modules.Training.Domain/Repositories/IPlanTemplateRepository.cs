using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IPlanTemplateRepository
{
    Task<PlanTemplate?> GetByIdAsync(Guid gymId, Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>Gym templates plus <paramref name="trainerId"/>'s own (none when null), not archived, newest first.</summary>
    Task<IReadOnlyList<PlanTemplate>> ListVisibleAsync(Guid gymId, Guid? trainerId, CancellationToken cancellationToken = default);

    Task<bool> AnyForGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task AddAsync(PlanTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds and saves seeded samples on its own. Returns 0 instead of failing when another request
    /// seeded the same gym at the same moment (the unique sample index rejects the duplicate).
    /// </summary>
    Task<int> TryAddSamplesAsync(IReadOnlyList<PlanTemplate> samples, CancellationToken cancellationToken = default);
}

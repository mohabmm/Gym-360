namespace GymApp.Common.Application.Tenancy;

public interface IGymRegistry
{
    Task<GymRegistration?> FindAsync(Guid gymId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a gym by its public 6-character GymCode. Walks active per-gym schemas
    /// to match (no denormalized column on gyms_registry today). Used by self-serve
    /// member enrollment / link-gym flows. Case-insensitive match.
    /// </summary>
    Task<GymRegistration?> FindByCodeAsync(string gymCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GymRegistration>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GymRegistration>> GetActiveAsync(CancellationToken cancellationToken = default);
}

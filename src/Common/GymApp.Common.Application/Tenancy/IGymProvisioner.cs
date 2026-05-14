namespace GymApp.Common.Application.Tenancy;

/// <summary>
/// Application-layer abstraction for per-gym tenant provisioning. Inserts the
/// gyms_registry row, allocates the next g{N} schema prefix, and creates the
/// per-module schemas + tables for the new gym. The Infrastructure layer
/// supplies the implementation (GymProvisioningService).
/// </summary>
public interface IGymProvisioner
{
    Task<GymRegistration> ProvisionAsync(
        Guid gymId,
        Guid ownerId,
        string name,
        string slug,
        string? gymCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses ProvisionAsync: drops the per-gym schemas (g{N}_*) and removes the
    /// gyms_registry row. No-op when the registry row is missing (soft mode or
    /// already-deleted gym).
    /// </summary>
    Task DeprovisionAsync(Guid gymId, CancellationToken cancellationToken = default);
}

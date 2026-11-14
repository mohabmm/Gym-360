namespace GymApp.Common.Application.Tenancy;

/// <summary>
/// Runs a unit of work once per tenant. Hard isolation: one fresh DI scope per registered
/// gym with the tenant context set, so per-gym DbContexts resolved from the scope target
/// that gym's <c>g{N}_*</c> schemas. Soft isolation: a single scope against the shared
/// schemas. Use for cross-gym maintenance (e.g. erasing a deleted user's data).
/// </summary>
public interface ITenantFanOut
{
    Task ForEachTenantAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken cancellationToken = default);
}

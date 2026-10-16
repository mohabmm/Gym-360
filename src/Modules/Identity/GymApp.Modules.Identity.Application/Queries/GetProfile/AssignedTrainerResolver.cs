using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.Queries.GetProfile;

/// <summary>
/// Resolves a trainee's assigned trainer per gym for <see cref="GetProfileQueryHandler"/>.
/// Identity is a global module — TenantResolutionMiddleware only sets a tenant
/// context when the JWT carries gymId+schemaPrefix (post select-gym). Without
/// that, ITrainerAssignmentLookup's BillingDbContext falls through to the
/// legacy `billing` schema and returns no rows for any tenant. Fan out per
/// gym instead so the trainee's "assigned trainer" resolves regardless of
/// whether they've selected a gym.
/// </summary>
internal sealed class AssignedTrainerResolver
{
    private readonly ITrainerAssignmentLookup _trainerAssignmentLookup;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGymRegistry _gymRegistry;
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ILogger<AssignedTrainerResolver> _logger;

    public AssignedTrainerResolver(
        ITrainerAssignmentLookup trainerAssignmentLookup,
        IServiceScopeFactory scopeFactory,
        IGymRegistry gymRegistry,
        ITenantContextAccessor tenantAccessor,
        ILogger<AssignedTrainerResolver> logger)
    {
        _trainerAssignmentLookup = trainerAssignmentLookup;
        _scopeFactory = scopeFactory;
        _gymRegistry = gymRegistry;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> ResolveAcrossGymsAsync(
        Guid memberId,
        IReadOnlyList<Guid> gymIds,
        CancellationToken cancellationToken)
    {
        // If the request already has a resolved tenant context (e.g. trainee called
        // select-gym), the existing single-shot lookup is correct and cheaper.
        if (IsSingleResolvedGym(gymIds))
            return await _trainerAssignmentLookup.GetAssignedTrainersAsync(memberId, gymIds, cancellationToken);

        var result = new Dictionary<Guid, Guid>();
        foreach (var gymId in gymIds)
        {
            var trainerId = await TryResolveForGymAsync(memberId, gymId, cancellationToken);
            if (trainerId.HasValue)
                result[gymId] = trainerId.Value;
        }
        return result;
    }

    private bool IsSingleResolvedGym(IReadOnlyList<Guid> gymIds)
    {
        var isSingleGym = gymIds.Count == 1;
        return _tenantAccessor.IsResolved && isSingleGym && gymIds[0] == _tenantAccessor.GymId;
    }

    private async Task<Guid?> TryResolveForGymAsync(Guid memberId, Guid gymId, CancellationToken cancellationToken)
    {
        try
        {
            var registration = await _gymRegistry.FindAsync(gymId, cancellationToken);
            if (registration is null) return null;

            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            accessor.Set(registration.GymId, registration.SchemaPrefix, isOwnerScope: false);

            var lookup = sp.GetRequiredService<ITrainerAssignmentLookup>();
            var perGym = await lookup.GetAssignedTrainersAsync(memberId, new[] { gymId }, cancellationToken);
            return perGym.TryGetValue(gymId, out var trainerId) ? trainerId : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "GetProfile: trainer-assignment lookup failed for member {MemberId} in gym {GymId}",
                memberId, gymId);
            return null;
        }
    }
}

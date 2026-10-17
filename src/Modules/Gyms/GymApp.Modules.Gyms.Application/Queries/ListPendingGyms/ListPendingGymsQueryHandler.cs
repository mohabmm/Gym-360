using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.ListPendingGyms;

internal sealed class ListPendingGymsQueryHandler : IQueryHandler<ListPendingGymsQuery, List<GymResponse>>
{
    private const string PendingApprovalStatus = "PendingApproval";

    private readonly IGymRegistry _registry;
    private readonly IGymQueryService _gymQueryService;
    private readonly ITenantContext _tenantContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ListPendingGymsQueryHandler> _logger;

    public ListPendingGymsQueryHandler(
        IGymRegistry registry,
        IGymQueryService gymQueryService,
        ITenantContext tenantContext,
        IServiceScopeFactory scopeFactory,
        ILogger<ListPendingGymsQueryHandler> logger)
    {
        _registry = registry;
        _gymQueryService = gymQueryService;
        _tenantContext = tenantContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Result<List<GymResponse>>> Handle(ListPendingGymsQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsHardIsolation)
        {
            var direct = await _gymQueryService.ListPendingGymsAsync(cancellationToken);
            return Result.Success(direct);
        }

        // Hard mode: walk gyms_registry, fan out per-gym, keep only aggregates
        // whose domain status is PendingApproval (registry status is
        // Active/Suspended — orthogonal to gym approval state).
        var registry = await _registry.GetActiveAsync(cancellationToken);
        var items = new List<GymResponse>();

        foreach (var reg in registry)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;

                var accessor = sp.GetRequiredService<ITenantContextAccessor>();
                accessor.Set(reg.GymId, reg.SchemaPrefix, isOwnerScope: false);

                var qs = sp.GetRequiredService<IGymQueryService>();
                var dto = await qs.GetGymByIdAsync(new GymId(reg.GymId), cancellationToken);
                if (dto is not null && string.Equals(dto.Status, PendingApprovalStatus, StringComparison.OrdinalIgnoreCase))
                    items.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Skipping gym {GymId} ({Prefix}) in pending-gyms fan-out: {Message}",
                    reg.GymId, reg.SchemaPrefix, ex.Message);
            }
        }

        return Result.Success(items);
    }
}

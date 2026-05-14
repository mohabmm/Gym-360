using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Gyms.Application.Queries.ListGyms;

internal sealed class ListGymsQueryHandler : IQueryHandler<ListGymsQuery, PagedResponse<GymResponse>>
{
    private readonly IGymRegistry _registry;
    private readonly IGymQueryService _gymQueryService;
    private readonly ITenantContext _tenantContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ListGymsQueryHandler> _logger;

    public ListGymsQueryHandler(
        IGymRegistry registry,
        IGymQueryService gymQueryService,
        ITenantContext tenantContext,
        IServiceScopeFactory scopeFactory,
        ILogger<ListGymsQueryHandler> logger)
    {
        _registry = registry;
        _gymQueryService = gymQueryService;
        _tenantContext = tenantContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Result<PagedResponse<GymResponse>>> Handle(ListGymsQuery request, CancellationToken cancellationToken)
    {
        // Soft mode: per-tenant query is correct (single shared schema).
        if (!_tenantContext.IsHardIsolation)
        {
            var direct = await _gymQueryService.ListGymsAsync(
                request.Page, request.PageSize, request.Name, cancellationToken);
            return Result.Success(direct);
        }

        // Hard mode: cross-tenant list reads gyms_registry, then fans out
        // per-gym to load each Gym aggregate from g{N}_gyms.
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize > 100 ? 100 : request.PageSize;

        var registry = await _registry.GetActiveAsync(cancellationToken);

        var filtered = string.IsNullOrWhiteSpace(request.Name)
            ? (IReadOnlyList<GymRegistration>)registry
            : registry
                .Where(g => g.Name.Contains(request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

        var totalCount = filtered.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var paged = filtered
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<GymResponse>(paged.Count);
        foreach (var reg in paged)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;

                var accessor = sp.GetRequiredService<ITenantContextAccessor>();
                accessor.Set(reg.GymId, reg.SchemaPrefix, isOwnerScope: false);

                var qs = sp.GetRequiredService<IGymQueryService>();
                var dto = await qs.GetGymByIdAsync(reg.GymId, cancellationToken);
                if (dto is not null) items.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Skipping gym {GymId} ({Prefix}) in list-gyms fan-out: {Message}",
                    reg.GymId, reg.SchemaPrefix, ex.Message);
            }
        }

        return Result.Success(new PagedResponse<GymResponse>(items, page, pageSize, totalCount, totalPages));
    }
}

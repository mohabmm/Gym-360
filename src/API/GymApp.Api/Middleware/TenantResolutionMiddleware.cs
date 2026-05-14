using System.Security.Claims;
using System.Text.Json;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using Microsoft.Extensions.Options;

namespace GymApp.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor accessor,
        IOptions<TenancyOptions> options)
    {
        if (options.Value.Mode == TenancyMode.Soft)
        {
            // Pass-through: no per-gym schema routing.
            await _next(context);
            return;
        }

        var routeGymId = TryGetRouteGymId(context);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var principalType = context.User.FindFirstValue("principalType");
            var isOwnerScope = string.Equals(principalType, "Owner", StringComparison.OrdinalIgnoreCase);
            var isPlatformAdmin = string.Equals(principalType, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);

            var gymClaim = context.User.FindFirstValue("gymId");
            if (Guid.TryParse(gymClaim, out var gymId))
            {
                // Cross-gym route mismatch: JWT is scoped to gym A, request path targets gym B.
                // Reject so an owner who has /select-gym'd into one gym cannot read or mutate
                // another gym's data by walking URLs. Platform admins may cross gyms freely.
                if (routeGymId.HasValue && routeGymId.Value != gymId && !isPlatformAdmin)
                {
                    await WriteCrossGymForbidden(context, gymId, routeGymId.Value);
                    accessor.Reset();
                    return;
                }

                // Schema prefix is supplied as a JWT claim by IdentityJwtTokenService once
                // the gym is selected — avoids a per-request lookup against gyms_registry.
                var prefix = context.User.FindFirstValue("schemaPrefix") ?? string.Empty;
                accessor.Set(gymId, prefix, isOwnerScope);
            }
            else if (isOwnerScope)
            {
                // Owner without a selected-gym JWT. If the route is gym-scoped (gymId in path),
                // resolve the schema from the registry after verifying ownership. Lets owner UIs
                // address gyms by URL without forcing a select-gym round-trip per gym switch.
                var ownerIdClaim = context.User.FindFirstValue("ownerId");
                // Default JwtBearer mapping rewrites "sub" → ClaimTypes.NameIdentifier;
                // try both for resilience.
                var subClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub");
                Guid.TryParse(ownerIdClaim, out var ownerAggregateId);
                Guid.TryParse(subClaim, out var ownerUserId);
                if (routeGymId.HasValue
                    && (ownerAggregateId != Guid.Empty || ownerUserId != Guid.Empty))
                {
                    var registry = context.RequestServices.GetService<IGymRegistry>();
                    if (registry is not null)
                    {
                        var reg = await registry.FindAsync(routeGymId.Value, context.RequestAborted);
                        // Ownership match: registry.OwnerId may be either the User.Id
                        // (admin-created gyms via /admin/owners flow) OR the Owner aggregate
                        // Id (legacy bootstrap migration). Accept both.
                        if (reg is not null && (reg.OwnerId == ownerAggregateId || reg.OwnerId == ownerUserId))
                        {
                            accessor.Set(reg.GymId, reg.SchemaPrefix, isOwnerScope: true);
                        }
                        else
                        {
                            // Owner does not own this gym, or gym not in registry. Leave unresolved
                            // so the gym-scoped guard below returns 404 instead of leaking SQL.
                            accessor.Set(null, string.Empty, true);
                        }
                    }
                    else
                    {
                        accessor.Set(null, string.Empty, true);
                    }
                }
                else
                {
                    // Non-gym-scoped owner request (e.g., chooser flow) — leave unresolved.
                    accessor.Set(null, string.Empty, true);
                }
            }
            else if (isPlatformAdmin)
            {
                // Platform admin has no gym claim. If the route is gym-scoped, prefer that gym so
                // queries hit the right schema. Otherwise default to the first active gym so
                // admin endpoints can still resolve a schema in Hard mode. Cross-tenant rollups
                // (PlatformAnalyticsReader, OwnerDashboardService) walk the registry directly and
                // do not depend on this fallback.
                var registry = context.RequestServices.GetService<IGymRegistry>();
                if (registry is not null)
                {
                    if (routeGymId.HasValue)
                    {
                        var reg = await registry.FindAsync(routeGymId.Value, context.RequestAborted);
                        if (reg is not null)
                        {
                            accessor.Set(reg.GymId, reg.SchemaPrefix, isOwnerScope: false);
                        }
                    }

                    if (!accessor.IsResolved)
                    {
                        var actives = await registry.GetActiveAsync(context.RequestAborted);
                        var first = actives.OrderBy(g => g.SequentialNumber).FirstOrDefault();
                        if (first is not null)
                            accessor.Set(first.GymId, first.SchemaPrefix, isOwnerScope: false);
                    }
                }
            }
        }

        // Gym-scoped guard: if the route carries a gymId and tenant resolution failed,
        // short-circuit with 404 rather than letting EF run a query against the default
        // "gyms" schema (which has no tables in Hard mode and produces a 500).
        if (routeGymId.HasValue && !accessor.IsResolved)
        {
            await WriteGymNotFound(context, routeGymId.Value);
            accessor.Reset();
            return;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            accessor.Reset();
        }
    }

    private static Guid? TryGetRouteGymId(HttpContext context)
    {
        var routeValue = context.GetRouteValue("gymId");
        if (routeValue is not null && Guid.TryParse(routeValue.ToString(), out var fromRoute))
            return fromRoute;
        return null;
    }

    private static async Task WriteCrossGymForbidden(HttpContext context, Guid jwtGymId, Guid routeGymId)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            data = (object?)null,
            errors = new[]
            {
                new
                {
                    code = "AUTH.CROSS_GYM_FORBIDDEN",
                    message = $"JWT is scoped to gym '{jwtGymId}'; request targets gym '{routeGymId}'. Call /api/v1/identity/select-gym to switch gyms.",
                    meta = (object?)null,
                },
            },
            meta = (object?)null,
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }

    private static async Task WriteGymNotFound(HttpContext context, Guid gymId)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            data = (object?)null,
            errors = new[]
            {
                new
                {
                    code = "GYM_NOT_FOUND",
                    message = $"Gym '{gymId}' not found or not accessible to the current principal.",
                    meta = (object?)null,
                },
            },
            meta = (object?)null,
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }
}

using System.Security.Claims;
using System.Text.Json;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using Microsoft.Extensions.Options;

namespace GymApp.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private static readonly JsonSerializerOptions ErrorJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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

        var request = new TenantRequest(context, accessor, TryGetRouteGymId(context));

        if (context.User.Identity?.IsAuthenticated == true
            && await ResolveAuthenticatedTenantAsync(request) == ResolutionOutcome.Rejected)
        {
            accessor.Reset();
            return;
        }

        // Gym-scoped guard: if the route carries a gymId and tenant resolution failed,
        // short-circuit with 404 rather than letting EF run a query against the default
        // "gyms" schema (which has no tables in Hard mode and produces a 500).
        if (request.RouteGymId.HasValue && !accessor.IsResolved)
        {
            await WriteErrorAsync(context, GymNotFound(request.RouteGymId.Value));
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

    /// <summary>Per-request state shared by the resolution steps.</summary>
    private sealed record TenantRequest(HttpContext Context, ITenantContextAccessor Accessor, Guid? RouteGymId)
    {
        public ClaimsPrincipal User => Context.User;

        public CancellationToken Aborted => Context.RequestAborted;

        public IGymRegistry? Registry => Context.RequestServices.GetService<IGymRegistry>();
    }

    private enum ResolutionOutcome
    {
        Continue,
        Rejected,
    }

    private static async Task<ResolutionOutcome> ResolveAuthenticatedTenantAsync(TenantRequest request)
    {
        var scope = PrincipalScope.From(request.User);

        if (Guid.TryParse(request.User.FindFirstValue("gymId"), out var gymId))
            return await ResolveFromGymClaimAsync(request, new GymClaim(gymId, scope));

        if (scope.IsOwnerScope)
            await ResolveOwnerFromRouteAsync(request);
        else if (scope.IsPlatformAdmin)
            await ResolvePlatformAdminAsync(request);

        return ResolutionOutcome.Continue;
    }

    private readonly record struct PrincipalScope(bool IsOwnerScope, bool IsPlatformAdmin)
    {
        public static PrincipalScope From(ClaimsPrincipal user)
        {
            var principalType = user.FindFirstValue("principalType");
            return new PrincipalScope(
                IsOwnerScope: string.Equals(principalType, "Owner", StringComparison.OrdinalIgnoreCase),
                IsPlatformAdmin: string.Equals(principalType, "PlatformAdmin", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>A parsed <c>gymId</c> JWT claim together with the principal's scope.</summary>
    private readonly record struct GymClaim(Guid GymId, PrincipalScope Scope)
    {
        // Platform admins may cross gyms freely.
        public bool ForbidsRouteGym(TenantRequest request) =>
            request.RouteGymId.HasValue && request.RouteGymId.Value != GymId && !Scope.IsPlatformAdmin;
    }

    private static async Task<ResolutionOutcome> ResolveFromGymClaimAsync(TenantRequest request, GymClaim claim)
    {
        // Cross-gym route mismatch: JWT is scoped to gym A, request path targets gym B.
        // Reject so an owner who has /select-gym'd into one gym cannot read or mutate
        // another gym's data by walking URLs.
        if (claim.ForbidsRouteGym(request))
        {
            await WriteErrorAsync(request.Context, CrossGymForbidden(claim.GymId, request.RouteGymId!.Value));
            return ResolutionOutcome.Rejected;
        }

        // Schema prefix is supplied as a JWT claim by IdentityJwtTokenService once
        // the gym is selected — avoids a per-request lookup against gyms_registry.
        var prefix = request.User.FindFirstValue("schemaPrefix") ?? string.Empty;
        request.Accessor.Set(claim.GymId, prefix, claim.Scope.IsOwnerScope);
        return ResolutionOutcome.Continue;
    }

    /// <summary>
    /// Owner without a selected-gym JWT. If the route is gym-scoped (gymId in path),
    /// resolve the schema from the registry after verifying ownership. Lets owner UIs
    /// address gyms by URL without forcing a select-gym round-trip per gym switch.
    /// </summary>
    private static async Task ResolveOwnerFromRouteAsync(TenantRequest request)
    {
        var ownedGym = await FindOwnedRouteGymAsync(request);

        // Owner does not own this gym, gym not in registry, or a non-gym-scoped owner
        // request (e.g., chooser flow): leave unresolved so the gym-scoped guard returns
        // 404 instead of leaking SQL.
        if (ownedGym is null)
            request.Accessor.Set(null, string.Empty, true);
        else
            request.Accessor.Set(ownedGym.GymId, ownedGym.SchemaPrefix, isOwnerScope: true);
    }

    private static async Task<GymRegistration?> FindOwnedRouteGymAsync(TenantRequest request)
    {
        var owner = OwnerIdentity.From(request.User);
        if (!request.RouteGymId.HasValue || !owner.IsKnown)
            return null;

        var registry = request.Registry;
        if (registry is null)
            return null;

        var reg = await registry.FindAsync(request.RouteGymId.Value, request.Aborted);
        return reg is not null && owner.Owns(reg) ? reg : null;
    }

    /// <summary>
    /// Ids an owner principal can be matched against <see cref="GymRegistration.OwnerId"/>.
    /// Registry.OwnerId may be either the User.Id (admin-created gyms via /admin/owners flow)
    /// OR the Owner aggregate Id (legacy bootstrap migration). Accept both.
    /// </summary>
    private readonly record struct OwnerIdentity(Guid OwnerAggregateId, Guid OwnerUserId)
    {
        public bool IsKnown => OwnerAggregateId != Guid.Empty || OwnerUserId != Guid.Empty;

        public bool Owns(GymRegistration reg) => reg.OwnerId == OwnerAggregateId || reg.OwnerId == OwnerUserId;

        public static OwnerIdentity From(ClaimsPrincipal user)
        {
            // Default JwtBearer mapping rewrites "sub" → ClaimTypes.NameIdentifier;
            // try both for resilience.
            var subClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            Guid.TryParse(user.FindFirstValue("ownerId"), out var ownerAggregateId);
            Guid.TryParse(subClaim, out var ownerUserId);
            return new OwnerIdentity(ownerAggregateId, ownerUserId);
        }
    }

    /// <summary>
    /// Platform admin has no gym claim. If the route is gym-scoped, prefer that gym so
    /// queries hit the right schema. Otherwise default to the first active gym so
    /// admin endpoints can still resolve a schema in Hard mode. Cross-tenant rollups
    /// (PlatformAnalyticsReader, OwnerDashboardService) walk the registry directly and
    /// do not depend on this fallback.
    /// </summary>
    private static async Task ResolvePlatformAdminAsync(TenantRequest request)
    {
        var registry = request.Registry;
        if (registry is null)
            return;

        if (request.RouteGymId.HasValue)
            SetAdminGym(request, await registry.FindAsync(request.RouteGymId.Value, request.Aborted));

        if (!request.Accessor.IsResolved)
            SetAdminGym(request, await FindFirstActiveGymAsync(registry, request.Aborted));
    }

    private static async Task<GymRegistration?> FindFirstActiveGymAsync(
        IGymRegistry registry,
        CancellationToken cancellationToken)
    {
        var actives = await registry.GetActiveAsync(cancellationToken);
        return actives.OrderBy(g => g.SequentialNumber).FirstOrDefault();
    }

    private static void SetAdminGym(TenantRequest request, GymRegistration? reg)
    {
        if (reg is not null)
            request.Accessor.Set(reg.GymId, reg.SchemaPrefix, isOwnerScope: false);
    }

    private static Guid? TryGetRouteGymId(HttpContext context)
    {
        var routeValue = context.GetRouteValue("gymId");
        if (routeValue is not null && Guid.TryParse(routeValue.ToString(), out var fromRoute))
            return fromRoute;
        return null;
    }

    private sealed record ErrorResponse(int StatusCode, string Code, string Message);

    private static ErrorResponse CrossGymForbidden(Guid jwtGymId, Guid routeGymId) => new(
        StatusCodes.Status403Forbidden,
        "AUTH.CROSS_GYM_FORBIDDEN",
        $"JWT is scoped to gym '{jwtGymId}'; request targets gym '{routeGymId}'. Call /api/v1/identity/select-gym to switch gyms.");

    private static ErrorResponse GymNotFound(Guid gymId) => new(
        StatusCodes.Status404NotFound,
        "GYM_NOT_FOUND",
        $"Gym '{gymId}' not found or not accessible to the current principal.");

    private static async Task WriteErrorAsync(HttpContext context, ErrorResponse error)
    {
        context.Response.StatusCode = error.StatusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            data = (object?)null,
            errors = new[]
            {
                new
                {
                    code = error.Code,
                    message = error.Message,
                    meta = (object?)null,
                },
            },
            meta = (object?)null,
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, ErrorJsonOptions));
    }
}

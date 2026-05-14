using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Authentication;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GymApp.Modules.Identity.Infrastructure.Services;

internal sealed class IdentityJwtTokenService : Application.Contracts.IJwtTokenService
{
    private readonly Common.Infrastructure.Authentication.IJwtTokenService _jwtTokenService;
    private readonly IGymRegistry _gymRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenancyOptions _tenancyOptions;
    private readonly JwtOptions _jwtOptions;

    public IdentityJwtTokenService(
        Common.Infrastructure.Authentication.IJwtTokenService jwtTokenService,
        IGymRegistry gymRegistry,
        IServiceScopeFactory scopeFactory,
        IOptions<TenancyOptions> tenancyOptions,
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtTokenService = jwtTokenService;
        _gymRegistry = gymRegistry;
        _scopeFactory = scopeFactory;
        _tenancyOptions = tenancyOptions.Value;
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// Resolves the gym-side StaffRole sub-role inside a fresh DI scope with tenant
    /// context pinned to the target gym's schema prefix. /login routes don't carry
    /// a selected gym in the JWT yet, so the request-scoped tenant accessor is
    /// unresolved and a direct resolver call would query the wrong schema.
    /// </summary>
    private async Task<string?> ResolveStaffSubRoleAsync(Guid gymId, string? schemaPrefix, Guid userId, CancellationToken ct)
    {
        if (gymId == Guid.Empty || userId == Guid.Empty) return null;

        using var scope = _scopeFactory.CreateScope();
        if (_tenancyOptions.Mode == TenancyMode.Hard && !string.IsNullOrEmpty(schemaPrefix))
        {
            var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            accessor.Set(gymId, schemaPrefix, isOwnerScope: false);
        }

        var resolver = scope.ServiceProvider.GetRequiredService<IStaffSubRoleResolver>();
        return await resolver.GetSubRoleAsync(gymId, userId, ct);
    }

    public async Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        string? adminRole = user.Role == UserRole.PlatformAdmin ? "super_admin" : null;
        string? staffRole = user.Role switch
        {
            UserRole.GymOwner => "owner",
            UserRole.GymStaff => "staff",
            _ => null
        };
        string? principalType = MapPrincipalType(user.Role);

        Guid? gymId = null;
        string? schemaPrefix = null;

        // Owner branch: if the user is an Owner principal with linked Owner aggregate,
        // skip the embedded gym claim — the Owner must select a gym via /api/auth/select-gym.
        // Owners with exactly 1 owned gym still need to pick (kept simple — UI auto-confirms).
        var isOwnerPrincipal = user.Role == UserRole.GymOwner && user.OwnerId.HasValue;
        if (!isOwnerPrincipal)
        {
            var activeMembership = user.GymMemberships?.FirstOrDefault(m => m.IsActive);
            gymId = activeMembership?.GymId;
            if (gymId.HasValue && _tenancyOptions.Mode == TenancyMode.Hard)
            {
                var reg = await _gymRegistry.FindAsync(gymId.Value, cancellationToken);
                schemaPrefix = reg?.SchemaPrefix;
            }
        }

        // Staff sub-role (Owner/Manager/Receptionist) — read from the gym's StaffRole
        // table when this is a staff token with a resolved gym. Surfaced as `staffSubRole`
        // claim for fine-grained authorization (e.g. gym_owner_or_manager policy).
        string? staffSubRole = null;
        if (user.Role == UserRole.GymStaff && gymId.HasValue)
        {
            staffSubRole = await ResolveStaffSubRoleAsync(gymId.Value, schemaPrefix, user.Id, cancellationToken);
        }

        var token = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email.Value,
            user.Role.ToRoleClaim(),
            gymId,
            staffRole,
            adminRole,
            principalType,
            user.OwnerId,
            schemaPrefix,
            staffSubRole);

        return token;
    }

    public async Task<string> GenerateAccessTokenForGymAsync(
        User user,
        Guid gymId,
        string schemaPrefix,
        CancellationToken cancellationToken = default)
    {
        string? adminRole = user.Role == UserRole.PlatformAdmin ? "super_admin" : null;
        string? staffRole = user.Role switch
        {
            UserRole.GymOwner => "owner",
            UserRole.GymStaff => "staff",
            _ => null
        };
        string? principalType = MapPrincipalType(user.Role);

        // Defensive: re-validate schema prefix against registry if running Hard.
        if (_tenancyOptions.Mode == TenancyMode.Hard)
        {
            var reg = await _gymRegistry.FindAsync(gymId, cancellationToken);
            if (reg is null) throw new InvalidOperationException($"Gym {gymId} not in registry.");
            schemaPrefix = reg.SchemaPrefix;
        }

        // Resolve gym-side StaffRole sub-role (Owner/Manager/Receptionist) for staff tokens.
        string? staffSubRole = null;
        if (user.Role == UserRole.GymStaff)
        {
            staffSubRole = await ResolveStaffSubRoleAsync(gymId, schemaPrefix, user.Id, cancellationToken);
        }

        return _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email.Value,
            user.Role.ToRoleClaim(),
            gymId,
            staffRole,
            adminRole,
            principalType,
            user.OwnerId,
            schemaPrefix,
            staffSubRole);
    }

    public Task<string> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = _jwtTokenService.GenerateRefreshToken();
        return Task.FromResult(token);
    }

    public DateTime GetRefreshTokenExpiryTime()
    {
        return DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);
    }

    public DateTime GetAccessTokenExpiryTime()
    {
        return DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
    }

    private static string? MapPrincipalType(UserRole role) => role switch
    {
        UserRole.GymOwner => "Owner",
        UserRole.GymStaff => "Staff",
        UserRole.Trainer => "Trainer",
        UserRole.Trainee => "Trainee",
        UserRole.PlatformAdmin => "PlatformAdmin",
        _ => null
    };
}

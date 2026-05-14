using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Identity.Infrastructure.Seed;

/// <summary>
/// Helpers the API host uses to bootstrap demo gym memberships without
/// exposing the internal IdentityDbContext across module boundaries.
/// </summary>
public static class IdentityDemoSupport
{
    public static async Task<Guid?> FindUserIdByEmailAsync(
        IServiceProvider services,
        string email,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var vo = Email.Create(email).Value;
        return await ctx.Users.AsNoTracking()
            .Where(u => u.Email == vo)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Links a user (found by email) to a gym with the given role, if not already linked.
    /// Returns true when a new membership was created.
    /// </summary>
    public static async Task<bool> LinkUserToGymAsync(
        IServiceProvider services,
        string email,
        Guid gymId,
        UserRole role,
        string? section,
        string? memberNumber = null,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var vo = Email.Create(email).Value;

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email == vo, ct);
        if (user is null) return false;

        if (user.GymMemberships.Any(m => m.GymId == gymId && m.Role == role && m.IsActive))
            return false;

        var result = user.LinkToGym(gymId, role, section, memberNumber);
        if (!result.IsSuccess) return false;

        await ctx.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Ensures an Owner aggregate row exists for the given user (by email) and that
    /// the user.OwnerId points at it. Returns the Owner.Id (creates if missing).
    /// Idempotent.
    /// </summary>
    public static async Task<Guid?> EnsureOwnerAggregateAsync(
        IServiceProvider services,
        string email,
        string companyName,
        string billingEmail,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var vo = Email.Create(email).Value;

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email == vo, ct);
        if (user is null) return null;

        // If already linked, return existing owner row.
        if (user.OwnerId is not null)
        {
            var existing = await ctx.Set<Owner>().FirstOrDefaultAsync(o => o.Id == user.OwnerId, ct);
            if (existing is not null) return existing.Id;
        }

        // Existing Owner row by user_id (legacy paths) — relink user instead of duplicating.
        var byUser = await ctx.Set<Owner>().FirstOrDefaultAsync(o => o.UserId == user.Id, ct);
        if (byUser is not null)
        {
            user.LinkToOwner(byUser.Id);
            await ctx.SaveChangesAsync(ct);
            return byUser.Id;
        }

        var owner = Owner.Create(user.Id, companyName, billingEmail);
        ctx.Set<Owner>().Add(owner);
        user.LinkToOwner(owner.Id);
        await ctx.SaveChangesAsync(ct);
        return owner.Id;
    }
}

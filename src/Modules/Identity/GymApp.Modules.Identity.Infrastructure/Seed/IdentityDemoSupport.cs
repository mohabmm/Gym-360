using GymApp.Modules.Identity.Domain.Entities;
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
        DemoGymLink link,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var user = await FindTrackedUserAsync(ctx, link.Email, ct);
        if (user is null || user.GymMemberships.Any(link.IsSatisfiedBy))
            return false;

        var result = user.LinkToGym(link.GymId, link.Role, link.Section, link.MemberNumber);
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
        DemoOwnerProfile profile,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var user = await FindTrackedUserAsync(ctx, profile.Email, ct);
        if (user is null) return null;

        // If already linked, return existing owner row.
        var linked = await FindLinkedOwnerAsync(ctx, user, ct);
        if (linked is not null) return linked.Id;

        // Existing Owner row by user_id (legacy paths) — relink user instead of duplicating.
        var owner = await ctx.Set<Owner>().FirstOrDefaultAsync(o => o.UserId == user.Id, ct)
            ?? AddOwner(ctx, user, profile);

        user.LinkToOwner(owner.Id);
        await ctx.SaveChangesAsync(ct);
        return owner.Id;
    }

    private static Task<User?> FindTrackedUserAsync(IdentityDbContext ctx, string email, CancellationToken ct)
    {
        var vo = Email.Create(email).Value;
        return ctx.Users.FirstOrDefaultAsync(u => u.Email == vo, ct);
    }

    private static async Task<Owner?> FindLinkedOwnerAsync(IdentityDbContext ctx, User user, CancellationToken ct)
    {
        if (user.OwnerId is null) return null;
        return await ctx.Set<Owner>().FirstOrDefaultAsync(o => o.Id == user.OwnerId, ct);
    }

    private static Owner AddOwner(IdentityDbContext ctx, User user, DemoOwnerProfile profile)
    {
        var owner = Owner.Create(user.Id, profile.CompanyName, profile.BillingEmail);
        ctx.Set<Owner>().Add(owner);
        return owner;
    }
}

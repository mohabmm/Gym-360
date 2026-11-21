using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Infrastructure.Seed;

public static class IdentityDevSeeder
{
    private sealed record DemoUser(string Email, string Phone, string First, string Last, Gender Gender, UserRole Role);

    private static readonly DemoUser[] AllSeeds =
    [
        new("trainee@demo.local", "+201000000001", "Demo", "Trainee", Gender.Male, UserRole.Trainee),
        new("trainer@demo.local", "+201000000002", "Demo", "Trainer", Gender.Male, UserRole.Trainer),
        new("staff@demo.local", "+201000000003", "Demo", "Staff", Gender.Female, UserRole.GymStaff),
        new("owner@demo.local", "+201000000004", "Demo", "Owner", Gender.Male, UserRole.GymOwner),
        new("admin@demo.local", "+201000000005", "Demo", "Admin", Gender.Male, UserRole.PlatformAdmin),
    ];

    /// <summary>
    /// Creates the demo accounts with <paramref name="password"/> and moves any existing
    /// demo account onto it, so changing Identity:DevSeederPassword rotates them on next boot.
    /// The password always comes from configuration — the repo is public, so no default lives here.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, string password, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var ctx = services.GetRequiredService<IdentityDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDevSeeder");
        var seeds = SelectSeeds(services.GetRequiredService<IConfiguration>());

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var added = 0;
        var rotated = 0;
        foreach (var seed in seeds)
        {
            var email = Email.Create(seed.Email).Value;
            var existing = await ctx.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (existing is null)
            {
                ctx.Users.Add(CreateUser(seed, email, hash));
                added++;
            }
            else if (!BCrypt.Net.BCrypt.Verify(password, existing.PasswordHash))
            {
                existing.UpdatePasswordHash(hash);
                rotated++;
            }
        }

        if (added + rotated > 0)
            await ctx.SaveChangesAsync(ct);

        logger.LogInformation(
            "IdentityDevSeeder: {Added} demo users added, {Rotated} moved to the configured password, {Total} total",
            added, rotated, seeds.Length);
    }

    private static DemoUser[] SelectSeeds(IConfiguration config)
    {
        var scope = config.GetValue<string>("Identity:DevSeederScope", "all") ?? "all";
        var adminOnly = string.Equals(scope, "admin", StringComparison.OrdinalIgnoreCase);
        return adminOnly
            ? AllSeeds.Where(s => s.Role == UserRole.PlatformAdmin).ToArray()
            : AllSeeds;
    }

    private static User CreateUser(DemoUser seed, Email email, string hash)
    {
        var user = User.Create(
            new NewUserDetails
            {
                Email = email,
                PhoneNumber = PhoneNumber.Create(seed.Phone).Value,
                PasswordHash = hash,
                FirstName = seed.First,
                LastName = seed.Last,
                Gender = seed.Gender
            },
            seed.Role);
        user.Activate();
        return user;
    }
}

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
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var sp = services;
        var ctx = sp.GetRequiredService<IdentityDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDevSeeder");
        var config = sp.GetRequiredService<IConfiguration>();
        var scope = config.GetValue<string>("Identity:DevSeederScope", "all") ?? "all";
        var adminOnly = string.Equals(scope, "admin", StringComparison.OrdinalIgnoreCase);

        // Password aligned with db/seed.sql + Playwright/MobAI fixtures (123456).
        var hash = BCrypt.Net.BCrypt.HashPassword("123456", workFactor: 12);

        var allSeeds = new (string Email, string Phone, string First, string Last, Gender Gender, UserRole Role)[]
        {
            ("trainee@demo.local",    "+201000000001", "Demo", "Trainee",  Gender.Male,   UserRole.Trainee),
            ("trainer@demo.local",    "+201000000002", "Demo", "Trainer",  Gender.Male,   UserRole.Trainer),
            ("staff@demo.local",      "+201000000003", "Demo", "Staff",    Gender.Female, UserRole.GymStaff),
            ("owner@demo.local",      "+201000000004", "Demo", "Owner",    Gender.Male,   UserRole.GymOwner),
            ("admin@demo.local",      "+201000000005", "Demo", "Admin",    Gender.Male,   UserRole.PlatformAdmin),
        };
        var seeds = adminOnly
            ? allSeeds.Where(s => s.Role == UserRole.PlatformAdmin).ToArray()
            : allSeeds;

        var added = 0;
        foreach (var s in seeds)
        {
            var email = Email.Create(s.Email).Value;
            var exists = await ctx.Users.AnyAsync(u => u.Email == email, ct);
            if (exists) continue;
            var phone = PhoneNumber.Create(s.Phone).Value;
            var user = User.Create(
                new NewUserDetails
                {
                    Email = email,
                    PhoneNumber = phone,
                    PasswordHash = hash,
                    FirstName = s.First,
                    LastName = s.Last,
                    Gender = s.Gender
                },
                s.Role);
            user.Activate();
            ctx.Users.Add(user);
            added++;
        }

        if (added == 0)
        {
            logger.LogInformation("IdentityDevSeeder: all {Count} demo users already exist", seeds.Length);
            return;
        }

        await ctx.SaveChangesAsync(ct);
        logger.LogInformation("IdentityDevSeeder: seeded {Added}/{Total} demo users (password: 123456)", added, seeds.Length);
    }
}

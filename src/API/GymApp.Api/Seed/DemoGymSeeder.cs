using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Billing.Infrastructure.Seed;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Api.Seed;

/// <summary>
/// Seeds a demo gym end-to-end so the platform is functional out of the box:
/// Gym (approved) → Pro subscription → session policy → Mens/Womens sections →
/// StaffRoles (owner + receptionist) → Identity gym memberships (owner/staff/trainer/trainee).
/// Idempotent: safe to run on every boot.
/// </summary>
public static class DemoGymSeeder
{
    private const string DemoGymCode = "DEMO01";
    private const string OwnerEmail = "owner@demo.local";
    private const string StaffEmail = "staff@demo.local";
    private const string TrainerEmail = "trainer@demo.local";
    private const string TraineeEmail = "trainee@demo.local";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var sp = services;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DemoGymSeeder");

        var gymsCtx = sp.GetRequiredService<GymsDbContext>();
        var billingCtx = sp.GetRequiredService<BillingDbContext>();

        var ownerUserId = await IdentityDemoSupport.FindUserIdByEmailAsync(services, OwnerEmail, ct);
        if (ownerUserId is null)
        {
            logger.LogWarning("DemoGymSeeder: owner user {Email} not found — skipping (IdentityDevSeeder must run first)", OwnerEmail);
            return;
        }

        // Ensure the Owner aggregate row exists and the user is linked. Gym.OwnerId
        // and gyms_registry.gyms.OwnerId both store the Owner aggregate id, NOT the
        // user id, so we must mint this before the Gym is created.
        var ownerAggregateId = await IdentityDemoSupport.EnsureOwnerAggregateAsync(
            services, OwnerEmail, "Demo Holdings", "owner@demo.local", ct);
        if (ownerAggregateId is null)
        {
            logger.LogWarning("DemoGymSeeder: failed to mint Owner aggregate for {Email} — skipping", OwnerEmail);
            return;
        }

        // Each per-gym schema (g{N}_gyms.gyms) holds exactly ONE gym row by design,
        // so taking the first tenant-scoped row is the right anchor for the seeder.
        // Matching by OwnerId/code would miss rows whose OwnerId points at a deleted
        // user from a legacy bootstrap, leading to duplicate "FirstGym" inserts.
        var gym = await gymsCtx.Gyms.FirstOrDefaultAsync(ct);

        if (gym is null)
        {
            gym = Gym.Create(
                ownerId: ownerAggregateId.Value,
                name: "FirstGym",
                description: "Default demo gym seeded for development",
                address: new Address(
                    Street: "1 El-Tahrir St",
                    City: "Cairo",
                    State: "Cairo",
                    Country: "EG",
                    ZipCode: "11511"),
                contactInfo: new ContactInfo(
                    Email: "info@demogym.local",
                    Phone: "+20223456789"),
                gymCode: GymCode.Create(DemoGymCode));

            gym.Approve();
            gymsCtx.Gyms.Add(gym);
            await gymsCtx.SaveChangesAsync(ct);
            logger.LogInformation("DemoGymSeeder: created gym {GymId} ({Code})", gym.Id, DemoGymCode);
        }

        // Cross-tenant registry: ensure OwnerId == owner aggregate id (legacy data
        // had the user id here, which broke owner-scoped middleware lookups).
        var registryDb = sp.GetRequiredService<TenantRegistryDbContext>();
        var reg = await registryDb.Gyms.FirstOrDefaultAsync(g => g.GymId == gym.Id, ct);
        if (reg is not null && reg.OwnerId != ownerAggregateId.Value)
        {
            reg.OwnerId = ownerAggregateId.Value;
            await registryDb.SaveChangesAsync(ct);
        }

        if (!await gymsCtx.GymSessionPolicies.AnyAsync(p => p.GymId == gym.Id, ct))
        {
            gymsCtx.GymSessionPolicies.Add(GymSessionPolicy.Create(gym.Id));
        }

        if (!await gymsCtx.GymSections.AnyAsync(s => s.GymId == gym.Id, ct))
        {
            gymsCtx.GymSections.Add(GymSection.Create(gym.Id, SectionType.Mens, "Men's Section"));
            gymsCtx.GymSections.Add(GymSection.Create(gym.Id, SectionType.Womens, "Women's Section"));
        }

        var staffSeeds = new (string Email, StaffRoleType Role)[]
        {
            (OwnerEmail, StaffRoleType.Owner),
            (StaffEmail, StaffRoleType.Receptionist),
        };

        foreach (var (email, role) in staffSeeds)
        {
            var userId = await IdentityDemoSupport.FindUserIdByEmailAsync(services, email, ct);
            if (userId is null) continue;

            var exists = await gymsCtx.StaffRoles.AnyAsync(r => r.GymId == gym.Id && r.UserId == userId.Value, ct);
            if (!exists)
                gymsCtx.StaffRoles.Add(StaffRole.Create(gym.Id, userId.Value, role));
        }

        await gymsCtx.SaveChangesAsync(ct);

        if (!await billingCtx.GymSubscriptions.AnyAsync(s => s.GymId == gym.Id, ct))
        {
            billingCtx.GymSubscriptions.Add(GymSubscription.Create(gym.Id, BillingSeedData.ProPlanId));
            await billingCtx.SaveChangesAsync(ct);
            logger.LogInformation("DemoGymSeeder: subscribed gym {GymId} to Pro plan", gym.Id);
        }

        var membershipSeeds = new (string Email, UserRole Role, string? Section)[]
        {
            (OwnerEmail,   UserRole.GymOwner, null),
            (StaffEmail,   UserRole.GymStaff, null),
            (TrainerEmail, UserRole.Trainer,  null),
            (TraineeEmail, UserRole.Trainee,  "Mens"),
        };

        // Trainees must be registered in gym_members BEFORE the Identity link so the
        // auto-generated MemberNumber flows into user_gym_memberships.member_number.
        // Without this the trainee app QR check-in screen shows "no member number".
        var memberDirectory = sp.GetRequiredService<IGymMemberDirectory>();
        var traineeNumbers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (email, role, section) in membershipSeeds)
        {
            if (role != UserRole.Trainee) continue;

            var traineeId = await IdentityDemoSupport.FindUserIdByEmailAsync(services, email, ct);
            if (traineeId is null) continue;

            var assigned = await memberDirectory.RegisterAsync(
                gym.Id, traineeId.Value, preferredNumber: null, section, ct);
            traineeNumbers[email] = assigned;

            // Mirror member into the Gym.MemberIds JSON so IsActiveMemberAsync
            // (which reads the JSON column, not gym_members) returns true.
            // AddMember is the authoritative path; gym_members alone leaves
            // /me/* endpoints returning NOT_A_MEMBER_OF_GYM.
            if (!gym.MemberIds.Contains(traineeId.Value))
                gym.AddMember(traineeId.Value);
        }
        await gymsCtx.SaveChangesAsync(ct);

        foreach (var (email, role, section) in membershipSeeds)
        {
            traineeNumbers.TryGetValue(email, out var memberNumber);
            await IdentityDemoSupport.LinkUserToGymAsync(
                services, email, gym.Id, role, section, memberNumber, ct);
        }

        logger.LogInformation("DemoGymSeeder: done ({GymId})", gym.Id);
    }
}

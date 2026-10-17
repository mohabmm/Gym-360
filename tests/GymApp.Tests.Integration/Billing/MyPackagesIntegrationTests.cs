using System.Net;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Billing;

/// <summary>
/// Integration tests for GET /api/v1/billing/me/packages.
/// Covers PRD acceptance criteria requiring real EF Core + MediatR:
/// membership check, suspension check, multi-gym scoping (Q3), own-data only.
/// </summary>
public sealed class MyPackagesIntegrationTests : BaseIntegrationTest
{
    public MyPackagesIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private async Task<Gym> SeedActiveGymWithMember(Guid memberId)
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = "Integration Gym",
                Description = null,
                Address = new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
                ContactInfo = new ContactInfo("gym@test.com", "+201234567890"),
                GymCode = GymCode.Generate()
            });
        gym.Approve();
        gym.AddMember(new MemberId(memberId));
        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();
        return gym;
    }

    private async Task<Gym> SeedSuspendedGymWithMember(Guid memberId)
    {
        var gym = await SeedActiveGymWithMember(memberId);
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        gym.Suspend("non-payment");
        gymsDb.Gyms.Update(gym);
        await gymsDb.SaveChangesAsync();
        return gym;
    }

    private async Task SeedPackage(Guid gymId, Guid memberId, string name = "10 Sessions")
    {
        var repo = Scope.ServiceProvider.GetRequiredService<ISessionPackageRepository>();
        var uow = Scope.ServiceProvider.GetRequiredService<IBillingUnitOfWork>();
        var pkg = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            MemberId = memberId,
            TemplateId = Guid.NewGuid(),
            Name = name,
            TotalSessions = 10,
            PricePerSession = Money.Create(500m),
            TotalPrice = Money.Create(5000m),
            AssignedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMonths(3)
        });
        await repo.AddAsync(pkg);
        await uow.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> GetMyPackagesAsync(Guid userId, Guid gymId)
    {
        var client = CreateTraineeClient(userId);
        client.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());
        return await client.GetAsync("api/v1/billing/me/packages");
    }

    [Fact]
    public async Task Should_Return200WithData_When_ActiveMemberHasPackages()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(userId);
        await SeedPackage(gym.Id, userId);

        var response = await GetMyPackagesAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return200WithEmptyArray_When_MemberHasNoPackages()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(userId);

        var response = await GetMyPackagesAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return403NotAMember_When_UserHasNoMembership()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(Guid.NewGuid());

        var response = await GetMyPackagesAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return403GymSuspended_When_GymIsSuspended()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedSuspendedGymWithMember(userId);

        var response = await GetMyPackagesAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_IsolateByGym_When_TraineeHasPackagesAtMultipleGyms()
    {
        // PRD Q3: multi-gym isolation.
        var userId = Guid.NewGuid();
        var gymA = await SeedActiveGymWithMember(userId);
        var gymB = await SeedActiveGymWithMember(userId);

        await SeedPackage(gymA.Id, userId, "Package A");
        await SeedPackage(gymB.Id, userId, "Package B");

        var response = await GetMyPackagesAsync(userId, gymB.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Package B");
        json.Should().NotContain("Package A",
            because: "Gym A package must not leak under X-Gym-Id=GymB (Q3 isolation)");
    }

    [Fact]
    public async Task Should_NotReturnOtherMembersPackages_When_AnotherMemberHasPackages()
    {
        // PRD: "Returns only the trainee's own packages (not other members')"
        var userId = Guid.NewGuid();
        var otherMember = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(userId);

        // Add the other member so we can create a package for them.
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var freshGym = await gymsDb.Gyms.FindAsync(gym.Id);
        freshGym!.AddMember(new MemberId(otherMember));
        await gymsDb.SaveChangesAsync();

        await SeedPackage(gym.Id, userId, "My Package");
        await SeedPackage(gym.Id, otherMember, "Other's Package");

        var response = await GetMyPackagesAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("My Package");
        json.Should().NotContain("Other's Package",
            because: "Trainee must only see their own packages");
    }
}

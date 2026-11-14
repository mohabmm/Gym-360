using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Communication.Infrastructure.Data;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Identity;

/// <summary>
/// Super Admin user console: suspend locks a user out on their next request,
/// reactivate lets them back in, and hard delete erases the user plus their
/// personal data across modules while payments are retained (PDPL / FOUNDER Q9).
/// </summary>
public sealed class AdminUserManagementIntegrationTests : BaseIntegrationTest
{
    private const string ProfileUrl = "api/v1/identity/profile";

    public AdminUserManagementIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static string UserUrl(Guid id) => $"api/v1/admin/users/{id}";

    private static string StatusUrl(Guid id) => $"{UserUrl(id)}/status";

    private async Task<User> SeedUserAsync(UserRole role = UserRole.Trainee)
    {
        using var scope = Factory.Services.CreateScope();
        var user = User.Create(
            new NewUserDetails
            {
                Email = Email.Create($"adm-{Guid.NewGuid():N}@test.com").Value,
                PhoneNumber = PhoneNumber.Create("+201011112222").Value,
                PasswordHash = "hashed",
                FirstName = "Salma",
                LastName = "Nabil",
                Gender = Gender.Female
            },
            role);
        user.Activate();
        await scope.ServiceProvider.GetRequiredService<IUserRepository>().AddAsync(user);
        await scope.ServiceProvider.GetRequiredService<IIdentityUnitOfWork>().SaveChangesAsync();
        return user;
    }

    private async Task SeedPersonalAndFinancialDataAsync(Guid userId, Guid gymId)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var progress = sp.GetRequiredService<ProgressDbContext>();
        progress.Streaks.Add(Streak.Create(userId, gymId, StreakType.DailyWorkout).Value);
        await progress.SaveChangesAsync();

        var communication = sp.GetRequiredService<CommunicationDbContext>();
        communication.Notifications.Add(Notification.Create(
            new NotificationDetails { UserId = userId, Type = NotificationType.General, GymId = gymId }, "t", "b"));
        communication.DeviceTokens.Add(UserDeviceToken.Create(userId, $"fcm-{Guid.NewGuid():N}", DevicePlatform.Android));
        await communication.SaveChangesAsync();

        await sp.GetRequiredService<IPaymentRepository>().AddAsync(Payment.Create(new PaymentDetails
        {
            GymId = gymId,
            MemberId = userId,
            Amount = Money.Create(300m),
            PaymentMethod = PaymentMethod.Cash,
            Purpose = PaymentPurpose.Membership,
            ReceiptNumber = $"REC-DEL-{Guid.NewGuid():N}".Substring(0, 20),
            RecordedBy = Guid.NewGuid(),
            ReferenceId = null
        }));
        await sp.GetRequiredService<IBillingUnitOfWork>().SaveChangesAsync();
    }

    [Fact]
    public async Task Suspend_LocksOutLiveToken_AndActivateRestoresAccess()
    {
        var user = await SeedUserAsync();
        var userClient = CreateTraineeClient(user.Id);
        var admin = CreateAdminClient();
        AssertAuthenticated(await userClient.GetAsync(ProfileUrl));

        var suspend = await admin.PatchAsJsonAsync(StatusUrl(user.Id), new { status = "suspended" });

        suspend.StatusCode.Should().Be(HttpStatusCode.OK);
        (await userClient.GetAsync(ProfileUrl)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadStatusAsync(user.Id)).Should().Be(UserStatus.Suspended);

        var activate = await admin.PatchAsJsonAsync(StatusUrl(user.Id), new { status = "active" });

        activate.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertAuthenticated(await userClient.GetAsync(ProfileUrl));
        (await ReadStatusAsync(user.Id)).Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task SetStatus_OwnAccount_IsRejected()
    {
        var me = await SeedUserAsync(UserRole.PlatformAdmin);

        var response = await CreateAdminClient(me.Id).PatchAsJsonAsync(StatusUrl(me.Id), new { status = "suspended" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadStatusAsync(me.Id)).Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task SetStatus_InvalidValue_ReturnsBadRequest()
    {
        var user = await SeedUserAsync();

        var response = await CreateAdminClient().PatchAsJsonAsync(StatusUrl(user.Id), new { status = "banned" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ErasesUserAndPersonalData_KeepsPayment_AndKillsLiveToken()
    {
        var user = await SeedUserAsync();
        var gymId = Guid.NewGuid();
        await SeedPersonalAndFinancialDataAsync(user.Id, gymId);
        var userClient = CreateTraineeClient(user.Id);

        var response = await CreateAdminClient().DeleteAsync(UserUrl(user.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        (await sp.GetRequiredService<IUserRepository>().GetByIdAsync(user.Id)).Should().BeNull();
        (await sp.GetRequiredService<ProgressDbContext>().Streaks.AnyAsync(s => s.UserId == user.Id)).Should().BeFalse();
        var communication = sp.GetRequiredService<CommunicationDbContext>();
        (await communication.Notifications.AnyAsync(n => n.UserId == user.Id)).Should().BeFalse();
        (await communication.DeviceTokens.AnyAsync(t => t.UserId == user.Id)).Should().BeFalse();
        (await sp.GetRequiredService<BillingDbContext>().Payments.AnyAsync(p => p.MemberId == user.Id))
            .Should().BeTrue("payments are financial records retained after deletion");
        (await userClient.GetAsync(ProfileUrl)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OwnerWhoStillOwnsGyms_ReturnsConflict()
    {
        var owner = await SeedUserAsync(UserRole.GymOwner);
        var registration = await SeedOwnedGymAsync(owner.Id);
        try
        {
            var response = await CreateAdminClient().DeleteAsync(UserUrl(owner.Id));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ReadStatusAsync(owner.Id)).Should().Be(UserStatus.Active);
        }
        finally
        {
            await RemoveRegistrationAsync(registration);
        }
    }

    [Fact]
    public async Task Delete_UnknownUser_ReturnsNotFound()
    {
        var response = await CreateAdminClient().DeleteAsync(UserUrl(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AsTrainee_IsForbidden()
    {
        var user = await SeedUserAsync();

        var response = await CreateTraineeClient().DeleteAsync(UserUrl(user.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<UserStatus?> ReadStatusAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetByIdAsync(userId);
        return user?.Status;
    }

    // Owner aggregate id is linked on the user; the registry row is what marks "owns a gym".
    private async Task<GymRegistration> SeedOwnedGymAsync(Guid ownerUserId)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var ownerId = Guid.NewGuid();

        var user = (await sp.GetRequiredService<IUserRepository>().GetByIdAsync(ownerUserId))!;
        user.LinkToOwner(ownerId);
        await sp.GetRequiredService<IIdentityUnitOfWork>().SaveChangesAsync();

        var sequence = Random.Shared.Next(900_000, 999_999);
        var registration = new GymRegistration
        {
            GymId = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Owned Gym",
            Slug = $"owned-{sequence}",
            SchemaPrefix = $"g{sequence}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        var registry = sp.GetRequiredService<TenantRegistryDbContext>();
        registry.Gyms.Add(registration);
        await registry.SaveChangesAsync();
        return registration;
    }

    private async Task RemoveRegistrationAsync(GymRegistration registration)
    {
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>().Gyms
            .Where(g => g.GymId == registration.GymId)
            .ExecuteDeleteAsync();
    }
}

using System.Net;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
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
/// Integration tests for GET /api/v1/billing/me/payments.
/// Covers PRD acceptance criteria requiring real EF Core + MediatR:
/// voided payments (Q9), multi-gym scoping (Q3), membership/suspension checks.
/// </summary>
public sealed class MyPaymentsIntegrationTests : BaseIntegrationTest
{
    public MyPaymentsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private async Task<Gym> SeedActiveGymWithMember(Guid memberId, string gymCode = "INT002")
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
        gym.AddMember(memberId);
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

    private sealed record PaymentSeed(decimal Amount = 500m, bool Voided = false, string ReceiptPrefix = "REC-TEST");

    private async Task<Payment> SeedPayment(Guid gymId, Guid memberId, PaymentSeed? seed = null)
    {
        var (amount, voided, receiptPrefix) = seed ?? new PaymentSeed();
        var repo = Scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var uow = Scope.ServiceProvider.GetRequiredService<IBillingUnitOfWork>();

        var payment = Payment.Create(new PaymentDetails
        {
            GymId = gymId,
            MemberId = memberId,
            Amount = Money.Create(amount),
            PaymentMethod = PaymentMethod.Cash,
            Purpose = PaymentPurpose.Membership,
            ReceiptNumber = $"{receiptPrefix}-{Guid.NewGuid():N}".Substring(0, 20),
            RecordedBy = Guid.NewGuid(),
            ReferenceId = null
        });

        if (voided)
        {
            payment.Void("Recorded against wrong member");
        }

        await repo.AddAsync(payment);
        await uow.SaveChangesAsync();
        return payment;
    }

    private async Task<HttpResponseMessage> GetMyPaymentsAsync(Guid userId, Guid gymId)
    {
        var client = CreateTraineeClient(userId);
        client.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());
        return await client.GetAsync("api/v1/billing/me/payments");
    }

    [Fact]
    public async Task Should_Return200WithData_When_ActiveMemberHasPayments()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(userId);
        await SeedPayment(gym.Id, userId);

        var response = await GetMyPaymentsAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return200WithEmptyArray_When_MemberHasNoPayments()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(userId);

        var response = await GetMyPaymentsAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return403NotAMember_When_UserHasNoMembership()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(Guid.NewGuid());

        var response = await GetMyPaymentsAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Return403GymSuspended_When_GymIsSuspended()
    {
        var userId = Guid.NewGuid();
        var gym = await SeedSuspendedGymWithMember(userId);

        var response = await GetMyPaymentsAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_IncludeVoidedPayments_When_VoidedPaymentsExist()
    {
        // PRD (Q9): Voided payments MUST be included in the response.
        var userId = Guid.NewGuid();
        var gym = await SeedActiveGymWithMember(userId);
        await SeedPayment(gym.Id, userId, new PaymentSeed(Amount: 1000m, Voided: false, ReceiptPrefix: "REC-OK"));
        await SeedPayment(gym.Id, userId, new PaymentSeed(Amount: 500m, Voided: true, ReceiptPrefix: "REC-VOID"));

        var response = await GetMyPaymentsAsync(userId, gym.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Voided",
            because: "PRD Q9: voided payments MUST remain visible in the trainee's audit trail");
        json.Should().NotContain("-500",
            because: "PRD Q9: voided payments must NEVER be represented as negative amounts");
    }

    [Fact]
    public async Task Should_IsolateByGym_When_TraineeHasPaymentsAtMultipleGyms()
    {
        // PRD Q3: multi-gym isolation.
        var userId = Guid.NewGuid();
        var gymA = await SeedActiveGymWithMember(userId);
        var gymB = await SeedActiveGymWithMember(userId);
        await SeedPayment(gymA.Id, userId, new PaymentSeed(Amount: 1111m, ReceiptPrefix: "REC-GYMA"));
        await SeedPayment(gymB.Id, userId, new PaymentSeed(Amount: 2222m, ReceiptPrefix: "REC-GYMB"));

        var response = await GetMyPaymentsAsync(userId, gymB.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("REC-GYMB");
        json.Should().NotContain("REC-GYMA",
            because: "Gym A payments must not leak under X-Gym-Id=GymB (Q3 isolation)");
    }
}

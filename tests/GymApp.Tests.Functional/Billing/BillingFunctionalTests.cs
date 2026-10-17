using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Billing;

public sealed class BillingFunctionalTests : BaseFunctionalTest
{
    public BillingFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    private const string AssignUrl = "api/v1/billing/packages/assign";
    private const string PaymentsUrl = "api/v1/billing/payments";

    #region POST /api/v1/billing/package-templates

    [Fact]
    public async Task CreatePackageTemplate_AsOwner_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateOwnerClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/billing/package-templates", new
        {
            GymId = gymId,
            Name = "10-Session Pack",
            SessionCount = 10,
            PricePerSession = 150.0
        });

        // Assert — the owner clears authorization; reaching the handler is the
        // contract under test. A 403 is still valid here: CreatePackageTemplate is
        // feature-gated on "session_packages", and an un-subscribed random gym
        // falls back to the free plan, so the handler legitimately returns 403.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePackageTemplate_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/billing/package-templates", new
        {
            GymId = Guid.NewGuid(),
            Name = "Test",
            SessionCount = 5,
            PricePerSession = 100.0
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/billing/packages/assign

    [Fact]
    public async Task AssignPackage_AsStaff_ProcessesRequest()
    {
        var gymId = Guid.NewGuid();

        AssertAuthenticated(await CreateStaffClient(gymId: gymId).PostAsJsonAsync(AssignUrl, new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            AssignedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMonths(3)
        }));
    }

    [Fact]
    public async Task AssignPackage_NoToken_Returns401() =>
        AssertStatusCode(
            await AnonymousClient.PostAsJsonAsync(AssignUrl, new
            {
                GymId = Guid.NewGuid(),
                TrainerId = Guid.NewGuid(),
                MemberId = Guid.NewGuid(),
                TemplateId = Guid.NewGuid(),
                AssignedBy = Guid.NewGuid()
            }),
            HttpStatusCode.Unauthorized);

    [Fact]
    public async Task AssignPackage_AsTrainee_Returns403()
    {
        // Arrange — a member must not be able to grant themselves a package.
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var client = CreateTraineeClient(memberId, gymId);

        // Act
        var response = await client.PostAsJsonAsync(AssignUrl, new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            MemberId = memberId,
            TemplateId = Guid.NewGuid(),
            AssignedBy = memberId
        });

        // Assert
        await AssertRejectedByPolicy(response);
    }

    #endregion

    #region POST /api/v1/billing/packages/quick-assign

    // A random test gym is on the free plan, so a caller that clears authorization
    // reaches the handler and gets the plan gate's 403 PLAN.FEATURE_NOT_AVAILABLE.
    // A caller rejected by the endpoint policy never gets that far.
    private static async Task AssertReachedHandler(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            (await response.Content.ReadAsStringAsync()).Should().Contain("FEATURE_NOT_AVAILABLE");
        }
    }

    private static async Task AssertRejectedByPolicy(HttpResponseMessage response)
    {
        AssertStatusCode(response, HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("FEATURE_NOT_AVAILABLE");
    }

    private static object QuickAssignBody(Guid gymId, Guid memberId) => new
    {
        GymId = gymId,
        TrainerId = Guid.NewGuid(),
        MemberId = memberId,
        SessionCount = 8,
        AssignedBy = memberId
    };

    [Fact]
    public async Task QuickAssignPackage_AsStaff_ClearsAuthorization()
    {
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        var response = await client.PostAsJsonAsync(
            "api/v1/billing/packages/quick-assign", QuickAssignBody(gymId, Guid.NewGuid()));

        await AssertReachedHandler(response);
    }

    [Fact]
    public async Task QuickAssignPackage_AsOwner_ClearsAuthorization()
    {
        var gymId = Guid.NewGuid();
        var client = CreateOwnerClient(gymId: gymId);

        var response = await client.PostAsJsonAsync(
            "api/v1/billing/packages/quick-assign", QuickAssignBody(gymId, Guid.NewGuid()));

        await AssertReachedHandler(response);
    }

    [Fact]
    public async Task QuickAssignPackage_AsTrainee_Returns403()
    {
        // Found on staging: a trainee token could POST a free 8-session package
        // for themselves and immediately book their trainer.
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var client = CreateTraineeClient(memberId, gymId);

        var response = await client.PostAsJsonAsync(
            "api/v1/billing/packages/quick-assign", QuickAssignBody(gymId, memberId));

        await AssertRejectedByPolicy(response);
    }

    [Fact]
    public async Task QuickAssignPackage_AsTrainer_Returns403()
    {
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(gymId: gymId);

        var response = await client.PostAsJsonAsync(
            "api/v1/billing/packages/quick-assign", QuickAssignBody(gymId, Guid.NewGuid()));

        await AssertRejectedByPolicy(response);
    }

    #endregion

    #region GET /api/v1/billing/plan-features

    [Fact]
    public async Task GetPlanFeatures_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/plan-features?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlanFeatures_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/billing/plan-features?gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Payments

    [Fact]
    public async Task RecordPayment_AsStaff_ProcessesRequest()
    {
        var gymId = Guid.NewGuid();

        AssertAuthenticated(await CreateStaffClient(gymId: gymId).PostAsJsonAsync(PaymentsUrl, new
        {
            GymId = gymId,
            MemberId = Guid.NewGuid(),
            Amount = 500.0,
            Currency = "EGP",
            PaymentMethod = "cash",
            Description = "Membership fee",
            ReceivedBy = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task RecordPayment_NoToken_Returns401() =>
        AssertStatusCode(
            await AnonymousClient.PostAsJsonAsync(PaymentsUrl, new
            {
                GymId = Guid.NewGuid(),
                MemberId = Guid.NewGuid(),
                Amount = 500.0,
                Currency = "EGP",
                PaymentMethod = "cash",
                ReceivedBy = Guid.NewGuid()
            }),
            HttpStatusCode.Unauthorized);

    [Fact]
    public async Task VoidPayment_AsOwner_ProcessesRequest()
    {
        // Arrange
        var client = CreateOwnerClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/billing/payments/{Guid.NewGuid()}/void",
            new { VoidedBy = Guid.NewGuid(), Reason = "Duplicate" });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Revenue

    [Fact]
    public async Task GetRevenue_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateOwnerClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/revenue?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRevenueByTrainer_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateOwnerClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/revenue/by-trainer?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}

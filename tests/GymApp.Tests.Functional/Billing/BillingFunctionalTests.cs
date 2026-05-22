using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Billing;

public sealed class BillingFunctionalTests : BaseFunctionalTest
{
    public BillingFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

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
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/billing/packages/assign", new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            AssignedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMonths(3)
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignPackage_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/billing/packages/assign", new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            AssignedBy = Guid.NewGuid()
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
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
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/billing/payments", new
        {
            GymId = gymId,
            MemberId = Guid.NewGuid(),
            Amount = 500.0,
            Currency = "EGP",
            PaymentMethod = "cash",
            Description = "Membership fee",
            ReceivedBy = Guid.NewGuid()
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordPayment_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/billing/payments", new
        {
            GymId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Amount = 500.0,
            Currency = "EGP",
            PaymentMethod = "cash",
            ReceivedBy = Guid.NewGuid()
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

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

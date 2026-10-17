using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Billing;

public sealed class BillingIntegrationTests : BaseIntegrationTest
{
    public BillingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private const string AssignUrl = "api/v1/billing/packages/assign";
    private const string PaymentsUrl = "api/v1/billing/payments";

    private async Task AssertAnonymousPostRejectedAsync(string url, object body) =>
        (await HttpClient.PostAsJsonAsync(url, body)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    #region Package Templates

    [Fact]
    public async Task CreatePackageTemplate_Authenticated_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateGymOwnerClient(gymId: gymId);

        var request = new
        {
            GymId = gymId,
            Name = "10-Session Package",
            SessionCount = 10,
            PricePerSession = 150.0m
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/billing/package-templates", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public Task CreatePackageTemplate_Unauthenticated_ReturnsUnauthorized() =>
        AssertAnonymousPostRejectedAsync(
            "api/v1/billing/package-templates",
            new { GymId = Guid.NewGuid(), Name = "Test", SessionCount = 5, PricePerSession = 100.0m });

    [Fact]
    public async Task GetPackageTemplates_Authenticated_ReturnsResponse()
    {
        // Arrange
        var client = CreateGymOwnerClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/package-templates?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Session Packages

    [Fact]
    public async Task AssignSessionPackage_Authenticated_ProcessesRequest()
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
    public Task AssignSessionPackage_Unauthenticated_ReturnsUnauthorized() =>
        AssertAnonymousPostRejectedAsync(AssignUrl, new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            AssignedBy = Guid.NewGuid()
        });

    [Fact]
    public async Task GetSessionPackages_Authenticated_ReturnsResponse()
    {
        // Arrange
        var client = CreateStaffClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/packages?gymId={Guid.NewGuid()}&memberId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessionPackages_GymWideWithoutMemberOrTrainer_ReturnsOk()
    {
        // The staff "Active Packages" screen lists the whole gym: only the gym
        // context is supplied, no member/trainer filter.
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);
        client.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());

        var response = await client.GetAsync("api/v1/billing/packages?status=Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Payments

    [Fact]
    public async Task RecordPayment_Authenticated_ProcessesRequest()
    {
        var gymId = Guid.NewGuid();

        AssertAuthenticated(await CreateStaffClient(gymId: gymId).PostAsJsonAsync(PaymentsUrl, new
        {
            GymId = gymId,
            MemberId = Guid.NewGuid(),
            Amount = 500.0m,
            Currency = "EGP",
            PaymentMethod = "cash",
            Description = "Monthly membership",
            ReceivedBy = Guid.NewGuid()
        }));
    }

    [Fact]
    public Task RecordPayment_Unauthenticated_ReturnsUnauthorized() =>
        AssertAnonymousPostRejectedAsync(PaymentsUrl, new
        {
            GymId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Amount = 500.0m,
            Currency = "EGP",
            PaymentMethod = "cash",
            ReceivedBy = Guid.NewGuid()
        });

    [Fact]
    public async Task GetPayments_Authenticated_ReturnsResponse()
    {
        // Arrange
        var client = CreateStaffClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/payments?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VoidPayment_Authenticated_ProcessesRequest()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var client = CreateGymOwnerClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/billing/payments/{paymentId}/void",
            new { VoidedBy = Guid.NewGuid(), Reason = "Duplicate entry" });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Plan Features

    [Fact]
    public async Task GetPlanFeatures_Authenticated_ReturnsResponse()
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
    public async Task GetPlanFeatures_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync(
            $"api/v1/billing/plan-features?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Revenue

    [Fact]
    public async Task GetRevenue_Authenticated_ReturnsResponse()
    {
        // Arrange
        var client = CreateGymOwnerClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/billing/revenue?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}

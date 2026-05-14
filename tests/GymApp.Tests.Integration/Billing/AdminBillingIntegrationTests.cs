using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Billing;

public sealed class AdminBillingIntegrationTests : BaseIntegrationTest
{
    public AdminBillingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Plans CRUD

    [Fact]
    public async Task CreatePlan_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();

        var request = new
        {
            Code = $"test-plan-{Guid.NewGuid():N}".Substring(0, 20),
            Name = "Test Plan",
            Description = "A test subscription plan",
            MonthlyPrice = 299.0m,
            AnnualPrice = 2990.0m,
            MemberLimit = 100,
            TrainerLimit = 10,
            StaffLimit = 5,
            IsPublic = true,
            SortOrder = 99
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/admin/plans", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePlan_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();

        var request = new
        {
            Code = "unauthorized",
            Name = "Unauthorized Plan",
            MonthlyPrice = 99.0m,
            AnnualPrice = 990.0m,
            MemberLimit = 10,
            TrainerLimit = 2,
            StaffLimit = 1,
            IsPublic = true,
            SortOrder = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/admin/plans", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllPlans_AsAdmin_ReturnsOk()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var response = await client.GetAsync("api/v1/admin/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllPlans_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync("api/v1/admin/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Plan Features

    [Fact]
    public async Task TogglePlanFeature_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();
        var planId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync(
            $"api/v1/admin/plans/{planId}/features/{featureId}",
            new { IsEnabled = true, LimitValue = (int?)null });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TogglePlanFeature_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"api/v1/admin/plans/{Guid.NewGuid()}/features/{Guid.NewGuid()}",
            new { IsEnabled = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFeatureMatrix_AsAdmin_ReturnsResponse()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var response = await client.GetAsync("api/v1/admin/feature-matrix");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Gym Feature Overrides

    [Fact]
    public async Task SetGymFeatureOverride_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        var request = new
        {
            GymId = gymId,
            FeatureCode = "custom_exercises",
            IsEnabled = true,
            Reason = "Premium support agreement"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/gyms/{gymId}/feature-overrides", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGymFeatureOverrides_AsAdmin_ReturnsResponse()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"api/v1/admin/gyms/{gymId}/feature-overrides");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveGymFeatureOverride_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync(
            $"api/v1/admin/gyms/{gymId}/feature-overrides/{overrideId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Deploy

    [Fact]
    public async Task DeployChanges_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();

        var request = new
        {
            DeployedBy = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/admin/deploy/global", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeployChanges_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/admin/deploy/global",
            new { DeployedBy = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeployChanges_WithTargetGym_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/deploy/gym/{gymId}",
            new { DeployedBy = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeployChanges_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/admin/deploy/global",
            new { DeployedBy = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Gym Subscription

    [Fact]
    public async Task GetGymSubscription_AsAdmin_ReturnsResponse()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"api/v1/admin/gyms/{gymId}/subscription");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeGymPlan_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        var request = new
        {
            GymId = gymId,
            NewPlanId = Guid.NewGuid(),
            Reason = "Upgrade requested",
            ChangedBy = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/gyms/{gymId}/change-plan", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Plan Change Log

    [Fact]
    public async Task GetPlanChangeLog_AsAdmin_ReturnsResponse()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"api/v1/admin/gyms/{gymId}/plan-change-log");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion
}

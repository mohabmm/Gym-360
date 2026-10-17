using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Billing;

public sealed class AdminBillingIntegrationTests : BaseIntegrationTest
{
    public AdminBillingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private const string PlansUrl = "api/v1/admin/plans";
    private const string GlobalDeployUrl = "api/v1/admin/deploy/global";

    private static string PlanFeatureUrl() =>
        $"api/v1/admin/plans/{Guid.NewGuid()}/features/{Guid.NewGuid()}";

    private static string GymUrl(Guid gymId, string action) => $"api/v1/admin/gyms/{gymId}/{action}";

    private static object DeployBody() => new { DeployedBy = Guid.NewGuid() };

    #region Plans CRUD

    [Fact]
    public async Task CreatePlan_AsAdmin_ProcessesRequest()
    {
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

        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(PlansUrl, request));
    }

    [Fact]
    public async Task CreatePlan_AsTrainee_ReturnsForbidden()
    {
        var response = await CreateTraineeClient().PostAsJsonAsync(PlansUrl, new
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
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllPlans_AsAdmin_ReturnsOk()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var response = await client.GetAsync(PlansUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllPlans_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(PlansUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Plan Features

    [Fact]
    public async Task TogglePlanFeature_AsAdmin_ProcessesRequest() =>
        AssertAuthorized(await CreateAdminClient().PutAsJsonAsync(
            PlanFeatureUrl(), new { IsEnabled = true, LimitValue = (int?)null }));

    [Fact]
    public async Task TogglePlanFeature_AsTrainee_ReturnsForbidden() =>
        (await CreateTraineeClient().PutAsJsonAsync(PlanFeatureUrl(), new { IsEnabled = true }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

    [Fact]
    public async Task GetFeatureMatrix_AsAdmin_ReturnsResponse() =>
        AssertAuthorized(await CreateAdminClient().GetAsync("api/v1/admin/feature-matrix"));

    #endregion

    #region Gym Feature Overrides

    [Fact]
    public async Task SetGymFeatureOverride_AsAdmin_ProcessesRequest()
    {
        var gymId = Guid.NewGuid();
        var request = new
        {
            GymId = gymId,
            FeatureCode = "custom_exercises",
            IsEnabled = true,
            Reason = "Premium support agreement"
        };

        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(GymUrl(gymId, "feature-overrides"), request));
    }

    [Fact]
    public async Task GetGymFeatureOverrides_AsAdmin_ReturnsResponse() =>
        AssertAuthorized(await CreateAdminClient().GetAsync(GymUrl(Guid.NewGuid(), "feature-overrides")));

    [Fact]
    public async Task RemoveGymFeatureOverride_AsAdmin_ProcessesRequest() =>
        AssertAuthorized(await CreateAdminClient().DeleteAsync(
            GymUrl(Guid.NewGuid(), $"feature-overrides/{Guid.NewGuid()}")));

    #endregion

    #region Deploy

    [Fact]
    public async Task DeployChanges_AsAdmin_ProcessesRequest() =>
        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(GlobalDeployUrl, DeployBody()));

    [Fact]
    public async Task DeployChanges_AsTrainee_ReturnsForbidden() =>
        (await CreateTraineeClient().PostAsJsonAsync(GlobalDeployUrl, DeployBody()))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

    [Fact]
    public async Task DeployChanges_WithTargetGym_AsAdmin_ProcessesRequest() =>
        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/admin/deploy/gym/{Guid.NewGuid()}", DeployBody()));

    [Fact]
    public async Task DeployChanges_Unauthenticated_ReturnsUnauthorized() =>
        (await HttpClient.PostAsJsonAsync(GlobalDeployUrl, DeployBody()))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    #endregion

    #region Gym Subscription

    [Fact]
    public async Task GetGymSubscription_AsAdmin_ReturnsResponse() =>
        AssertAuthorized(await CreateAdminClient().GetAsync(GymUrl(Guid.NewGuid(), "subscription")));

    [Fact]
    public async Task ChangeGymPlan_AsAdmin_ProcessesRequest()
    {
        var gymId = Guid.NewGuid();
        var request = new
        {
            GymId = gymId,
            NewPlanId = Guid.NewGuid(),
            Reason = "Upgrade requested",
            ChangedBy = Guid.NewGuid()
        };

        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(GymUrl(gymId, "change-plan"), request));
    }

    #endregion

    #region Plan Change Log

    [Fact]
    public async Task GetPlanChangeLog_AsAdmin_ReturnsResponse() =>
        AssertAuthorized(await CreateAdminClient().GetAsync(GymUrl(Guid.NewGuid(), "plan-change-log")));

    #endregion
}

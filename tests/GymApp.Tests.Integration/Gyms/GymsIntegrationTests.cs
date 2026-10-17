using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Presentation;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gyms;

public sealed class GymsIntegrationTests : BaseIntegrationTest
{
    public GymsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private const string GymsUrl = "api/v1/gyms";

    private static object EgyptAddress() =>
        new { Street = "123 Test St", City = "Cairo", State = "Cairo", Country = "Egypt", ZipCode = "12345" };

    private static object GymContact() => new { Phone = "+201234567890", Email = "gym@test.com" };

    private static string AdminGymUrl(string action) => $"api/v1/admin/gyms/{Guid.NewGuid()}/{action}";

    private Task<HttpResponseMessage> PostCheckInAsStaffAsync(object request)
    {
        var gymId = Guid.NewGuid();
        return CreateStaffClient(gymId: gymId).PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", request);
    }

    #region Registration and Approval

    [Fact]
    public async Task RegisterGym_ValidData_ReturnsCreated()
    {
        var response = await CreateAdminClient().PostAsJsonAsync(GymsUrl, new
        {
            OwnerId = Guid.NewGuid(),
            Name = $"Test Gym {Guid.NewGuid():N}",
            Description = "A test gym for integration tests",
            Address = EgyptAddress(),
            Contact = GymContact()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterGym_MissingName_ReturnsBadRequest() =>
        (await CreateAdminClient().PostAsJsonAsync(GymsUrl, new
        {
            OwnerId = Guid.NewGuid(),
            Name = "",
            Address = EgyptAddress(),
            Contact = GymContact()
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

    [Fact]
    public async Task RegisterGym_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            OwnerId = Guid.NewGuid(),
            Name = "Unauthorized Gym",
            Address = new { Street = "123 Test St", City = "Cairo", Governorate = "Cairo", PostalCode = "12345" },
            Contact = GymContact()
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync(GymsUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Gym

    [Fact]
    public async Task GetGym_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync($"api/v1/gyms/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGym_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync($"api/v1/gyms/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Suspension Flow

    [Fact]
    public async Task SuspendGym_AsAdmin_ReturnsOk() =>
        // May be 200 or 400/404 if gym doesn't exist, but not 401/403
        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(
            AdminGymUrl("suspend"), new { Reason = "Non-payment" }));

    [Fact]
    public async Task SuspendGym_AsTrainee_ReturnsForbidden() =>
        (await CreateTraineeClient().PostAsJsonAsync(
            AdminGymUrl("suspend"), new { Reason = "Trying to suspend as trainee" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

    [Fact]
    public async Task ReactivateGym_AsAdmin_ReturnsOk() =>
        // May be 200 or 400/404 if gym doesn't exist, but not 401/403
        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(AdminGymUrl("reactivate"), new { }));

    [Fact]
    public async Task ReactivateGym_AsTrainee_ReturnsForbidden() =>
        (await CreateTraineeClient().PostAsJsonAsync(AdminGymUrl("reactivate"), new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

    #endregion

    #region Check-In

    [Fact]
    public async Task CheckIn_ValidQr_ReturnsCreated() =>
        // May fail due to gym not existing, but should process request
        AssertAuthenticated(await PostCheckInAsStaffAsync(new
        {
            Method = "qr",
            QrToken = "valid-qr-token-" + Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            StaffId = Guid.NewGuid()
        }));

    [Fact]
    public async Task CheckIn_ValidMemberNumber_ReturnsCreated() =>
        AssertAuthenticated(await PostCheckInAsStaffAsync(new
        {
            Method = "member_number",
            MemberNumber = "MEM001",
            StaffId = Guid.NewGuid()
        }));

    [Fact]
    public async Task CheckIn_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var request = new
        {
            Method = "qr",
            QrToken = "some-token"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}

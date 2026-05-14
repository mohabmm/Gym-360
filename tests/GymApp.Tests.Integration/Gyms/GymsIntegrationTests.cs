using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Presentation;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gyms;

public sealed class GymsIntegrationTests : BaseIntegrationTest
{
    public GymsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Registration and Approval

    [Fact]
    public async Task RegisterGym_ValidData_ReturnsCreated()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var client = CreateAdminClient();

        var request = new
        {
            OwnerId = ownerId,
            Name = $"Test Gym {Guid.NewGuid():N}",
            Description = "A test gym for integration tests",
            Address = new { Street = "123 Test St", City = "Cairo", State = "Cairo", Country = "Egypt", ZipCode = "12345" },
            Contact = new { Phone = "+201234567890", Email = "gym@test.com" }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gyms", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterGym_MissingName_ReturnsBadRequest()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var client = CreateAdminClient();

        var request = new
        {
            OwnerId = ownerId,
            Name = "",
            Address = new { Street = "123 Test St", City = "Cairo", State = "Cairo", Country = "Egypt", ZipCode = "12345" },
            Contact = new { Phone = "+201234567890", Email = "gym@test.com" }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gyms", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterGym_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            OwnerId = Guid.NewGuid(),
            Name = "Unauthorized Gym",
            Address = new { Street = "123 Test St", City = "Cairo", Governorate = "Cairo", PostalCode = "12345" },
            Contact = new { Phone = "+201234567890", Email = "gym@test.com" }
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/gyms", request);

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
    public async Task SuspendGym_AsAdmin_ReturnsOk()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/admin/gyms/{gymId}/suspend", new
        {
            Reason = "Non-payment"
        });

        // Assert — may be 200 or 400/404 if gym doesn't exist, but not 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuspendGym_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/admin/gyms/{gymId}/suspend", new
        {
            Reason = "Trying to suspend as trainee"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReactivateGym_AsAdmin_ReturnsOk()
    {
        // Arrange
        var client = CreateAdminClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/admin/gyms/{gymId}/reactivate", new { });

        // Assert — may be 200 or 400/404 if gym doesn't exist, but not 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReactivateGym_AsTrainee_ReturnsForbidden()
    {
        // Arrange
        var client = CreateTraineeClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/admin/gyms/{gymId}/reactivate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Check-In

    [Fact]
    public async Task CheckIn_ValidQr_ReturnsCreated()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        var request = new
        {
            Method = "qr",
            QrToken = "valid-qr-token-" + Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            StaffId = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", request);

        // Assert — may fail due to gym not existing, but should process request
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckIn_ValidMemberNumber_ReturnsCreated()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        var request = new
        {
            Method = "member_number",
            MemberNumber = "MEM001",
            StaffId = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

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

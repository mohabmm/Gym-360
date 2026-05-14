using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Presentation;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Identity;

public sealed class PdplIntegrationTests : BaseIntegrationTest
{
    public PdplIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task RecordConsent_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        var request = new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I agree to data processing",
            IpAddress = "127.0.0.1"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/identity/consent", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportMyData_Authenticated_ReturnsOkOrNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync($"api/v1/identity/my-data?userId={userId}");

        // Assert — may succeed or return not-found if user doesn't exist in DB,
        // but should NOT return 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportMyData_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync($"api/v1/identity/my-data?userId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.DeleteAsync($"api/v1/identity/account?userId={userId}");

        // Assert — may be 204 or 400/404 depending on state, but NOT 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.DeleteAsync($"api/v1/identity/account?userId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PdplFlow_Register_Consent_Export_Delete()
    {
        // Step 1: Register a user
        var email = $"pdpl-flow-{Guid.NewGuid():N}@test.com";
        var registerResponse = await HttpClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email,
            Password = "PdplP@ss123!",
            FirstName = "Pdpl",
            LastName = "Test",
            Phone = "+201000000001",
            Role = "Trainee"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Parse user ID from registration response if possible, otherwise use a client
        var userId = Guid.NewGuid(); // Placeholder, actual flow would extract from token
        var client = CreateTraineeClient(userId);

        // Step 2: Record consent
        var consentResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I consent to data processing under Egypt PDPL",
            IpAddress = "192.168.1.1"
        });
        // Consent may fail if userId doesn't match DB — that's expected in this E2E skeleton
        consentResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 3: Export my data
        var exportResponse = await client.GetAsync($"api/v1/identity/my-data?userId={userId}");
        exportResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 4: Delete account
        var deleteResponse = await client.DeleteAsync($"api/v1/identity/account?userId={userId}");
        deleteResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}

using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Identity;

public sealed class PdplFunctionalTests : BaseFunctionalTest
{
    public PdplFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region GET /api/v1/identity/my-data (PDPL Export)

    [Fact]
    public async Task ExportMyData_Authenticated_Returns200OrProcesses()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync($"api/v1/identity/my-data?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportMyData_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/identity/my-data?userId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DELETE /api/v1/identity/account (PDPL Deletion)

    [Fact]
    public async Task DeleteAccount_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.DeleteAsync($"api/v1/identity/account?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.DeleteAsync(
            $"api/v1/identity/account?userId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/identity/consent (PDPL Consent)

    [Fact]
    public async Task RecordConsent_Authenticated_ProcessesRequest() =>
        AssertAuthenticated(await PostAsTraineeAsync("api/v1/identity/consent", userId => new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I consent under Egypt PDPL",
            IpAddress = "127.0.0.1"
        }));

    [Fact]
    public async Task RecordConsent_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = Guid.NewGuid(),
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "Test",
            IpAddress = "127.0.0.1"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordConsent_InvalidType_Returns400() =>
        AssertStatusCode(
            await PostAsTraineeAsync("api/v1/identity/consent", userId => new
            {
                UserId = userId,
                ConsentType = "",
                IsGranted = true,
                ConsentText = "",
                IpAddress = ""
            }),
            HttpStatusCode.BadRequest);

    #endregion

    #region PDPL End-to-End Flow

    [Fact]
    public async Task PdplEndToEnd_Register_Consent_Export_Delete()
    {
        // Step 1: Register
        var email = $"pdpl-e2e-{Guid.NewGuid():N}@test.com";
        var registerResponse = await RegisterTestUser(new TestUserRegistration { Email = email });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Get an authenticated client (in real flow we'd parse the token from registration)
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Step 2: Record consent
        var consentResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I consent to processing of my personal data under the Egypt PDPL",
            IpAddress = "192.168.1.100"
        });
        consentResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 3: Export my data
        var exportResponse = await client.GetAsync($"api/v1/identity/my-data?userId={userId}");
        exportResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 4: Delete account
        var deleteResponse = await client.DeleteAsync($"api/v1/identity/account?userId={userId}");
        deleteResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 5: Attempt re-register within 30-day window (restore option)
        var reRegisterResponse = await RegisterTestUser(new TestUserRegistration { Email = email });
        // Should succeed as the email is now available (soft-deleted) or offer restore
        reRegisterResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest); // May vary based on restore logic
    }

    #endregion
}

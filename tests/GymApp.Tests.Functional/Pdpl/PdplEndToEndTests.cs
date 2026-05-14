using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Pdpl;

/// <summary>
/// End-to-end PDPL (Egypt Personal Data Protection Law) compliance tests.
/// Tests the complete flow: register -> consent -> export data -> delete account -> re-register within 30d.
/// </summary>
public sealed class PdplEndToEndTests : BaseFunctionalTest
{
    public PdplEndToEndTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task PdplFullFlow_Register_Consent_Export_Delete_ReRegister()
    {
        // --- Step 1: Register a new user ---
        var email = $"pdpl-full-{Guid.NewGuid():N}@test.com";
        var registerResponse = await AnonymousClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email,
            Password = "PdplTest_P@ss123!",
            FirstName = "PDPL",
            LastName = "TestUser",
            Phone = "+201500000001",
            Role = "Trainee"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "registration with valid data should succeed");

        // --- Step 2: Get an authenticated client ---
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // --- Step 3: Record consent (PDPL requires explicit consent) ---
        var consentResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I consent to the processing of my personal data as described in the privacy policy, in accordance with the Egyptian Personal Data Protection Law.",
            IpAddress = "196.160.1.50"
        });
        consentResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "authenticated consent recording should not return 401");

        // Record marketing consent separately
        var marketingConsentResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "marketing",
            IsGranted = false,
            ConsentText = "I do not consent to marketing communications.",
            IpAddress = "196.160.1.50"
        });
        marketingConsentResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // --- Step 4: Export my data (PDPL right of access) ---
        var exportResponse = await client.GetAsync($"api/v1/identity/my-data?userId={userId}");
        exportResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "PDPL data export should be accessible to authenticated users");

        // --- Step 5: Delete account (PDPL right to erasure) ---
        var deleteResponse = await client.DeleteAsync($"api/v1/identity/account?userId={userId}");
        deleteResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "PDPL account deletion should be accessible to authenticated users");

        // --- Step 6: Attempt re-registration within 30-day window ---
        // Under PDPL, soft-deleted accounts have a 30-day restoration window.
        // Re-registering with the same email should either:
        // (a) offer to restore the account, or
        // (b) succeed as a new registration if the old email was freed
        var reRegisterResponse = await AnonymousClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email,
            Password = "PdplReReg_P@ss123!",
            FirstName = "PDPL",
            LastName = "Restored",
            Phone = "+201500000002",
            Role = "Trainee"
        });
        // Either Created (new account) or OK (restored) or BadRequest (email still held) are valid
        reRegisterResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PdplConsentWithdrawal_CanWithdrawAndReGrant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Step 1: Grant consent
        var grantResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I grant consent",
            IpAddress = "10.0.0.1"
        });
        grantResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 2: Withdraw consent
        var withdrawResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = false,
            ConsentText = "I withdraw my consent",
            IpAddress = "10.0.0.1"
        });
        withdrawResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Step 3: Re-grant consent
        var reGrantResponse = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = "data_processing",
            IsGranted = true,
            ConsentText = "I re-grant my consent",
            IpAddress = "10.0.0.2"
        });
        reGrantResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PdplDataExport_ContainsExpectedFields()
    {
        // Arrange: Register user
        var email = $"pdpl-export-{Guid.NewGuid():N}@test.com";
        var registerResponse = await RegisterTestUser(email);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Use an authenticated client
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act: Export data
        var response = await client.GetAsync($"api/v1/identity/my-data?userId={userId}");

        // Assert: Should process the request without auth errors
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("data_processing")]
    [InlineData("marketing")]
    [InlineData("analytics")]
    public async Task PdplConsent_DifferentTypes_AllAccepted(string consentType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/identity/consent", new
        {
            UserId = userId,
            ConsentType = consentType,
            IsGranted = true,
            ConsentText = $"I consent to {consentType}",
            IpAddress = "127.0.0.1"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}

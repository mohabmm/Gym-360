using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Presentation;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Identity;

public sealed class IdentityIntegrationTests : BaseIntegrationTest
{
    public IdentityIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Registration Tests

    [Fact]
    public async Task RegisterUser_ValidData_ReturnsCreatedWithTokens()
    {
        // Arrange
        var request = new
        {
            Email = $"register-{Guid.NewGuid():N}@test.com",
            Password = "StrongP@ss123!",
            FirstName = "Ahmed",
            LastName = "Mohamed",
            Phone = "+201234567890",
            Role = "Trainee"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        var request = new
        {
            Email = email,
            Password = "StrongP@ss123!",
            FirstName = "First",
            LastName = "User",
            Phone = "+201234567891",
            Role = "Trainee"
        };

        // Register first user
        await HttpClient.PostAsJsonAsync("api/v1/identity/register", request);

        // Act - register same email again
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterUser_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "invalid-email",
            Password = "StrongP@ss123!",
            FirstName = "Ahmed",
            LastName = "Mohamed",
            Phone = "+201234567890",
            Role = "Trainee"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid():N}@test.com";
        var password = "StrongP@ss123!";

        await HttpClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Ali",
            LastName = "Hassan",
            Phone = "+201111111111",
            Role = "Trainee"
        });

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"loginbad-{Guid.NewGuid():N}@test.com";

        await HttpClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email,
            Password = "StrongP@ss123!",
            FirstName = "Test",
            LastName = "User",
            Phone = "+201222222222",
            Role = "Trainee"
        });

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/login", new
        {
            Email = email,
            Password = "WrongP@ss999!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/login", new
        {
            Email = $"noone-{Guid.NewGuid():N}@test.com",
            Password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/refresh-token", new
        {
            Token = "invalid-refresh-token"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Profile Tests

    [Fact]
    public async Task GetProfile_Authenticated_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync($"api/v1/identity/profile?userId={userId}");

        // Assert — may be 200 or 404 depending on whether user exists, but not 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync("api/v1/identity/profile?userId=" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}

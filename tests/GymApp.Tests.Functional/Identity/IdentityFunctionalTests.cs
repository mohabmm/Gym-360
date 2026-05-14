using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Identity;

public sealed class IdentityFunctionalTests : BaseFunctionalTest
{
    public IdentityFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region POST /api/v1/identity/register

    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        // Act
        var response = await RegisterTestUser();

        // Assert
        AssertStatusCode(response, HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = "",
            Password = "StrongP@ss123!",
            FirstName = "Test",
            LastName = "User",
            Phone = "+201234567890",
            Role = "Trainee"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_MissingPassword_Returns400()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = $"nopwd-{Guid.NewGuid():N}@test.com",
            Password = "",
            FirstName = "Test",
            LastName = "User",
            Phone = "+201234567890",
            Role = "Trainee"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("12345")]
    [InlineData("abc")]
    public async Task Register_WeakPassword_Returns400(string password)
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = $"weakpwd-{Guid.NewGuid():N}@test.com",
            Password = password,
            FirstName = "Test",
            LastName = "User",
            Phone = "+201234567890",
            Role = "Trainee"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflictOrBadRequest()
    {
        // Arrange
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        await RegisterTestUser(email);

        // Act
        var response = await RegisterTestUser(email);

        // Assert - could be 400 or 409 depending on implementation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("Trainee")]
    [InlineData("Trainer")]
    public async Task Register_ValidRoles_Returns201(string role)
    {
        // Act
        var response = await RegisterTestUser(role: role);

        // Assert
        AssertStatusCode(response, HttpStatusCode.Created);
    }

    #endregion

    #region POST /api/v1/identity/login

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid():N}@test.com";
        var password = "StrongP@ss123!";
        await RegisterTestUser(email, password);

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var email = $"loginfail-{Guid.NewGuid():N}@test.com";
        await RegisterTestUser(email, "StrongP@ss123!");

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/login", new
        {
            Email = email,
            Password = "WrongP@ssword!"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/login", new
        {
            Email = $"nobody-{Guid.NewGuid():N}@test.com",
            Password = "P@ssword123!"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/identity/refresh-token

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/identity/refresh-token", new
        {
            Token = "invalid-token"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/identity/profile

    [Fact]
    public async Task GetProfile_Authenticated_DoesNotReturn401()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync($"api/v1/identity/profile?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/identity/profile?userId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion
}

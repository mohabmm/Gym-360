using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymApp.Common.Infrastructure.Authentication;
using GymApp.Common.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Functional.Abstractions;

[Collection(nameof(FunctionalTestCollection))]
public abstract class BaseFunctionalTest : IAsyncLifetime
{
    protected readonly FunctionalTestWebAppFactory Factory;
    protected readonly HttpClient AnonymousClient;
    private readonly IServiceScope _scope;

    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory)
    {
        Factory = factory;
        _scope = factory.Services.CreateScope();
        AnonymousClient = factory.CreateClient();
    }

    /// <summary>
    /// Creates a JWT token for testing.
    /// </summary>
    protected string CreateToken(
        Guid userId,
        string email = "test@example.com",
        string role = "trainee",
        Guid? gymId = null,
        string? staffRole = null,
        string? adminRole = null)
    {
        var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtService.GenerateAccessToken(userId, email, role, gymId, staffRole, adminRole);
    }

    /// <summary>
    /// Creates an HttpClient authenticated as a specific role.
    /// </summary>
    protected HttpClient CreateClient(
        string role = "trainee",
        Guid? userId = null,
        Guid? gymId = null,
        string? staffRole = null,
        string? adminRole = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var token = CreateToken(uid, $"{role}-{uid}@test.com", role, gymId, staffRole, adminRole);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient authenticated as a platform admin (super_admin).
    /// </summary>
    protected HttpClient CreateAdminClient(Guid? userId = null)
        => CreateClient("admin", userId, adminRole: "super_admin");

    /// <summary>
    /// Creates an HttpClient authenticated as a support-level admin.
    /// </summary>
    protected HttpClient CreateSupportClient(Guid? userId = null)
        => CreateClient("admin", userId, adminRole: "support");

    /// <summary>
    /// Creates an HttpClient authenticated as an analyst-level admin.
    /// </summary>
    protected HttpClient CreateAnalystClient(Guid? userId = null)
        => CreateClient("admin", userId, adminRole: "analyst");

    /// <summary>
    /// Creates an HttpClient authenticated as a trainee.
    /// </summary>
    protected HttpClient CreateTraineeClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient("trainee", userId, gymId);

    /// <summary>
    /// Creates an HttpClient authenticated as a trainer.
    /// </summary>
    protected HttpClient CreateTrainerClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient("trainer", userId, gymId);

    /// <summary>
    /// Creates an HttpClient authenticated as a gym owner.
    /// </summary>
    protected HttpClient CreateOwnerClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient("gym_owner", userId, gymId, staffRole: "owner");

    /// <summary>
    /// Creates an HttpClient authenticated as gym staff.
    /// </summary>
    protected HttpClient CreateStaffClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient("gym_staff", userId, gymId, staffRole: "staff");

    /// <summary>
    /// Asserts the response status code matches the expected code.
    /// </summary>
    protected static void AssertStatusCode(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected,
            because: $"expected {expected} but got {response.StatusCode} for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}");
    }

    /// <summary>
    /// Asserts the response has a success status code (2xx).
    /// </summary>
    protected static void AssertSuccess(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            because: $"expected success but got {response.StatusCode} for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}");
    }

    /// <summary>
    /// Registers a test user and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> RegisterTestUser(
        string? email = null,
        string password = "StrongP@ss123!",
        string role = "Trainee")
    {
        var request = new
        {
            Email = email ?? $"func-{Guid.NewGuid():N}@test.com",
            Password = password,
            FirstName = "Test",
            LastName = "User",
            Phone = "+201234567890",
            Role = role
        };
        return await AnonymousClient.PostAsJsonAsync("api/v1/identity/register", request);
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync()
    {
        _scope.Dispose();
        AnonymousClient.Dispose();
        return Task.CompletedTask;
    }
}

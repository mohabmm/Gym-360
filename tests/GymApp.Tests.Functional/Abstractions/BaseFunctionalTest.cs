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
    /// Creates a JWT token for testing. The principal's <see cref="TestPrincipal.UserId"/>
    /// must already be resolved.
    /// </summary>
    protected string CreateToken(TestPrincipal principal)
    {
        var userId = principal.UserId!.Value;
        var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtService.GenerateAccessToken(
            new AccessTokenClaims(userId, $"{principal.Role}-{userId}@test.com", principal.Role)
            {
                GymId = principal.GymId,
                StaffRole = principal.StaffRole,
                AdminRole = principal.AdminRole
            });
    }

    /// <summary>
    /// Creates an HttpClient authenticated as the given principal.
    /// </summary>
    protected HttpClient CreateClient(TestPrincipal principal)
    {
        var token = CreateToken(principal with { UserId = principal.UserId ?? Guid.NewGuid() });
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient authenticated as a platform admin (super_admin).
    /// </summary>
    protected HttpClient CreateAdminClient()
        => CreateClient(new TestPrincipal("admin") { AdminRole = "super_admin" });

    /// <summary>
    /// Creates an HttpClient authenticated as a support-level admin.
    /// </summary>
    protected HttpClient CreateSupportClient()
        => CreateClient(new TestPrincipal("admin") { AdminRole = "support" });

    /// <summary>
    /// Creates an HttpClient authenticated as an analyst-level admin.
    /// </summary>
    protected HttpClient CreateAnalystClient()
        => CreateClient(new TestPrincipal("admin") { AdminRole = "analyst" });

    /// <summary>
    /// Creates an HttpClient authenticated as a trainee.
    /// </summary>
    protected HttpClient CreateTraineeClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient(new TestPrincipal("trainee") { UserId = userId, GymId = gymId });

    /// <summary>
    /// Creates an HttpClient authenticated as a trainer.
    /// </summary>
    protected HttpClient CreateTrainerClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient(new TestPrincipal("trainer") { UserId = userId, GymId = gymId });

    /// <summary>
    /// Creates an HttpClient authenticated as a gym owner.
    /// </summary>
    protected HttpClient CreateOwnerClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient(new TestPrincipal("gym_owner") { UserId = userId, GymId = gymId, StaffRole = "owner" });

    /// <summary>
    /// Creates an HttpClient authenticated as gym staff.
    /// </summary>
    protected HttpClient CreateStaffClient(Guid? userId = null, Guid? gymId = null)
        => CreateClient(new TestPrincipal("gym_staff") { UserId = userId, GymId = gymId, StaffRole = "staff" });

    /// <summary>
    /// Sends a GET as a fresh trainee whose user id is threaded into the URL.
    /// </summary>
    protected Task<HttpResponseMessage> GetAsTraineeAsync(Func<Guid, string> urlForUser)
    {
        var userId = Guid.NewGuid();
        return CreateTraineeClient(userId).GetAsync(urlForUser(userId));
    }

    /// <summary>
    /// Sends a JSON POST as a fresh trainee whose user id is threaded into the body.
    /// </summary>
    protected Task<HttpResponseMessage> PostAsTraineeAsync(string url, Func<Guid, object> bodyForUser)
    {
        var userId = Guid.NewGuid();
        return CreateTraineeClient(userId).PostAsJsonAsync(url, bodyForUser(userId));
    }

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
    /// Asserts the request was authenticated (not 401) — authorization and the
    /// handler itself may still reject it.
    /// </summary>
    protected static void AssertAuthenticated(HttpResponseMessage response)
        => response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

    /// <summary>
    /// Asserts the request got past authentication and authorization
    /// (neither 401 nor 403) — the handler itself may still reject it.
    /// </summary>
    protected static void AssertAuthorized(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Registers a test user and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> RegisterTestUser(TestUserRegistration? registration = null)
    {
        registration ??= new TestUserRegistration();
        var request = new
        {
            Email = registration.Email ?? $"func-{Guid.NewGuid():N}@test.com",
            Password = registration.Password,
            FirstName = "Test",
            LastName = "User",
            Phone = "+201234567890",
            Role = registration.Role
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

using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Infrastructure.Authentication;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly ISender Sender;
    protected readonly IServiceScope Scope;
    protected readonly HttpClient HttpClient;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Scope = factory.Services.CreateScope();
        Sender = Scope.ServiceProvider.GetRequiredService<ISender>();
        HttpClient = factory.CreateClient();
    }

    /// <summary>
    /// Creates a JWT access token for testing with the principal's claims. The
    /// principal's <see cref="TestPrincipal.UserId"/> must already be resolved.
    /// </summary>
    protected string CreateAccessToken(TestPrincipal principal)
    {
        var userId = principal.UserId!.Value;
        var jwtService = Scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtService.GenerateAccessToken(
            new AccessTokenClaims(userId, $"user-{userId}@test.com", principal.Role)
            {
                GymId = principal.GymId,
                StaffRole = principal.StaffRole,
                AdminRole = principal.AdminRole,
                StaffSubRole = principal.StaffSubRole
            });
    }

    /// <summary>
    /// Creates an authenticated HttpClient for the given principal.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(TestPrincipal principal)
    {
        var token = CreateAccessToken(principal with { UserId = principal.UserId ?? Guid.NewGuid() });
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a platform admin.
    /// </summary>
    protected HttpClient CreateAdminClient(Guid? userId = null)
        => CreateAuthenticatedClient(new TestPrincipal("admin") { UserId = userId, AdminRole = "super_admin" });

    /// <summary>
    /// Creates an authenticated HttpClient for a gym owner.
    /// </summary>
    protected HttpClient CreateGymOwnerClient(Guid? userId = null, Guid? gymId = null)
        => CreateAuthenticatedClient(
            new TestPrincipal("gym_owner") { UserId = userId, GymId = gymId, StaffRole = "owner" });

    /// <summary>
    /// Creates an authenticated HttpClient for a trainer.
    /// </summary>
    protected HttpClient CreateTrainerClient(Guid? userId = null, Guid? gymId = null)
        => CreateAuthenticatedClient(new TestPrincipal("trainer") { UserId = userId, GymId = gymId });

    /// <summary>
    /// Creates an authenticated HttpClient for a trainee.
    /// </summary>
    protected HttpClient CreateTraineeClient(Guid? userId = null, Guid? gymId = null)
        => CreateAuthenticatedClient(new TestPrincipal("trainee") { UserId = userId, GymId = gymId });

    /// <summary>
    /// Creates an authenticated HttpClient for gym staff.
    /// </summary>
    protected HttpClient CreateStaffClient(Guid? userId = null, Guid? gymId = null)
        => CreateAuthenticatedClient(
            new TestPrincipal("gym_staff") { UserId = userId, GymId = gymId, StaffRole = "staff" });

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
    /// Resolve a service from DI.
    /// </summary>
    protected T GetService<T>() where T : notnull
        => Scope.ServiceProvider.GetRequiredService<T>();

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync()
    {
        Scope.Dispose();
        HttpClient.Dispose();
        return Task.CompletedTask;
    }
}

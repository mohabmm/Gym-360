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
    /// Creates a JWT access token for testing with the specified claims.
    /// </summary>
    protected string CreateAccessToken(
        Guid userId,
        string email = "test@example.com",
        string role = "trainee",
        Guid? gymId = null,
        string? staffRole = null,
        string? adminRole = null,
        string? staffSubRole = null)
    {
        var jwtService = Scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtService.GenerateAccessToken(
            userId, email, role, gymId, staffRole, adminRole, staffSubRole: staffSubRole);
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a trainee user.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(
        Guid? userId = null,
        string role = "trainee",
        Guid? gymId = null,
        string? staffRole = null,
        string? adminRole = null,
        string? staffSubRole = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var token = CreateAccessToken(
            uid, $"user-{uid}@test.com", role, gymId, staffRole, adminRole, staffSubRole);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a platform admin.
    /// </summary>
    protected HttpClient CreateAdminClient(Guid? userId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return CreateAuthenticatedClient(uid, "admin", adminRole: "super_admin");
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a gym owner.
    /// </summary>
    protected HttpClient CreateGymOwnerClient(Guid? userId = null, Guid? gymId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return CreateAuthenticatedClient(uid, "gym_owner", gymId, staffRole: "owner");
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a trainer.
    /// </summary>
    protected HttpClient CreateTrainerClient(Guid? userId = null, Guid? gymId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return CreateAuthenticatedClient(uid, "trainer", gymId);
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a trainee.
    /// </summary>
    protected HttpClient CreateTraineeClient(Guid? userId = null, Guid? gymId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return CreateAuthenticatedClient(uid, "trainee", gymId);
    }

    /// <summary>
    /// Creates an authenticated HttpClient for gym staff.
    /// </summary>
    protected HttpClient CreateStaffClient(Guid? userId = null, Guid? gymId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return CreateAuthenticatedClient(uid, "gym_staff", gymId, staffRole: "staff");
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

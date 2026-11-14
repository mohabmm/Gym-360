using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Identity;

/// <summary>
/// Staff app owner flow over HTTP: login → select-gym → (access token expires) refresh → the
/// refreshed token is still scoped to the gym, and the gym's empty trainer list loads as [].
/// Before the fix, select-gym never saved its refresh token, so the refresh failed and the
/// staff app showed "Error loading trainers".
/// </summary>
public sealed class OwnerGymSessionIntegrationTests : BaseIntegrationTest
{
    private const string Password = "Owner#2026pass";

    public OwnerGymSessionIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Owner_session_keeps_its_gym_across_refresh_and_an_empty_trainer_list_loads()
    {
        var (email, ownerId) = await CreateOwnerAsync();
        var gymId = await CreateOwnedGymAsync(ownerId);

        var login = await PostDataAsync(HttpClient, "api/v1/identity/login", new { email, password = Password });

        var owner = Factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        var selected = await PostDataAsync(owner, "api/v1/identity/select-gym", new { gymId });
        GymClaim(selected.GetProperty("accessToken").GetString()!).Should().Be(gymId.ToString());

        var refreshed = await PostDataAsync(HttpClient, "api/v1/identity/refresh-token",
            new { token = selected.GetProperty("refreshToken").GetString() });
        var refreshedAccess = refreshed.GetProperty("accessToken").GetString()!;
        GymClaim(refreshedAccess).Should().Be(gymId.ToString(), "refresh keeps the gym the owner selected");
        refreshed.GetProperty("user").GetProperty("gymId").GetGuid().Should().Be(gymId);

        // A second refresh (rotated token) still works and still carries the gym.
        var again = await PostDataAsync(HttpClient, "api/v1/identity/refresh-token",
            new { token = refreshed.GetProperty("refreshToken").GetString() });
        GymClaim(again.GetProperty("accessToken").GetString()!).Should().Be(gymId.ToString());

        var staffApp = Factory.CreateClient();
        staffApp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccess);
        var trainers = await staffApp.GetAsync($"api/v1/gyms/{gymId}/trainers");
        trainers.StatusCode.Should().Be(HttpStatusCode.OK, await trainers.Content.ReadAsStringAsync());
        var body = JsonDocument.Parse(await trainers.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("data").GetArrayLength().Should().Be(0, "no trainers is an empty list, not an error");
    }

    private async Task<(string Email, Guid OwnerAggregateId)> CreateOwnerAsync()
    {
        var email = $"owner-{Guid.NewGuid():N}@test.local";
        var ownerAggregateId = Guid.NewGuid();
        var user = User.Create(new NewUserDetails
        {
            Email = Email.Create(email).Value,
            PhoneNumber = PhoneNumber.Create($"+2010{Random.Shared.Next(10000000, 99999999)}").Value,
            PasswordHash = GetService<IPasswordHasher>().Hash(Password),
            FirstName = "Mona",
            LastName = "Adel",
            Gender = Gender.Female,
        }, UserRole.GymOwner);
        user.LinkToOwner(ownerAggregateId);
        user.Activate();

        await GetService<IUserRepository>().AddAsync(user);
        await GetService<IIdentityUnitOfWork>().SaveChangesAsync();
        return (email, ownerAggregateId);
    }

    private async Task<Guid> CreateOwnedGymAsync(Guid ownerAggregateId)
    {
        using var scope = Factory.Services.CreateScope();
        var gymsDb = scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(Guid.NewGuid(), new GymDetails
        {
            Name = "Owner Session Gym",
            Description = null,
            Address = new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
            ContactInfo = new ContactInfo("owner-gym@test.com", "+201234567890"),
            GymCode = GymCode.Generate(),
        });
        gym.Approve();
        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();

        var registry = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>();
        var suffix = gym.Id.ToString("N")[..8];
        registry.Gyms.Add(new GymRegistration
        {
            GymId = gym.Id,
            OwnerId = ownerAggregateId,
            Name = $"Owner Gym {suffix}",
            Slug = $"owner-{suffix}",
            SchemaPrefix = $"g{Random.Shared.Next(100000, 999999)}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        });
        await registry.SaveChangesAsync();
        return gym.Id;
    }

    private static async Task<JsonElement> PostDataAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        var text = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"{url} → {(int)response.StatusCode} {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    /// <summary>The <c>gymId</c> claim of a JWT (payload is base64url JSON).</summary>
    private static string? GymClaim(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload))).RootElement;
        return json.TryGetProperty("gymId", out var claim) ? claim.GetString() : null;
    }
}

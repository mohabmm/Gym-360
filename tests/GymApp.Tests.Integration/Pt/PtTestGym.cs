using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>Shared setup for PT integration tests: an approved pt_module gym and JSON helpers.</summary>
internal static class PtTestGym
{
    /// <summary>Creates an approved gym (with the trainee as an active member), registers it and applies pt_module.</summary>
    public static async Task<Guid> RegisterAsync(IServiceProvider services, Guid traineeId)
    {
        using var scope = services.CreateScope();

        // A real, approved gym with the trainee as an active member (trainee /me endpoints check membership).
        var gymsDb = scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = "PT Pilot Gym",
                Description = null,
                Address = new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
                ContactInfo = new ContactInfo("pt@test.com", "+201234567890"),
                GymCode = GymCode.Generate(),
            });
        gym.Approve();
        gym.AddMember(new MemberId(traineeId));
        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();

        var registry = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>();
        var suffix = gym.Id.ToString("N")[..8];
        registry.Gyms.Add(new GymRegistration
        {
            GymId = gym.Id,
            OwnerId = Guid.NewGuid(),
            Name = $"PT Gym {suffix}",
            Slug = $"pt-{suffix}",
            SchemaPrefix = $"g{Random.Shared.Next(100000, 999999)}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        });
        await registry.SaveChangesAsync();

        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetGymPresetAsync(new[] { gym.Id }, FeatureRegistry.PtModulePreset, new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();

        return gym.Id;
    }

    /// <summary>GETs the URL, asserts success and returns the <c>data</c> element.</summary>
    public static async Task<JsonElement> DataAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"{url} → {(int)response.StatusCode} {body}");
        return JsonDocument.Parse(body).RootElement.GetProperty("data").Clone();
    }
}

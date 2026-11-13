using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Tests.Functional.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Functional.FeatureFlags;

/// <summary>
/// End-to-end over HTTP: Super Admin-only flag API, per-gym resolution (override &gt; preset &gt; OFF),
/// 403 FEATURE_DISABLED enforcement, isolation between gyms, and the audit log.
/// </summary>
public sealed class FeatureFlagFunctionalTests : BaseFunctionalTest
{
    private const string AdminBase = "api/v1/admin/feature-flags";
    private const string WaterLogUrl = "api/v1/nutrition/water-log";

    public FeatureFlagFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    [Theory]
    [InlineData("admin")]
    [InlineData("support")]
    [InlineData("analyst")]
    public async Task Non_super_admin_platform_admins_get_403(string adminRole)
    {
        var client = CreateClient(new TestPrincipal("admin") { AdminRole = adminRole });

        var response = await client.GetAsync($"{AdminBase}/gyms");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("trainee", null)]
    [InlineData("trainer", null)]
    [InlineData("gym_staff", "owner")]
    public async Task Gym_roles_get_403_on_the_admin_flag_api(string role, string? staffRole)
    {
        var client = CreateClient(new TestPrincipal(role) { GymId = Guid.NewGuid(), StaffRole = staffRole });

        var response = await client.PutAsJsonAsync($"{AdminBase}/gyms/{Guid.NewGuid()}/preset", new { presetCode = "business" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_gets_401()
    {
        var response = await AnonymousClient.GetAsync($"{AdminBase}/gyms");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Super_admin_can_list_catalog_and_presets()
    {
        var admin = CreateAdminClient();

        var catalog = await ReadDataAsync(await admin.GetAsync($"{AdminBase}/catalog"));
        var presets = await ReadDataAsync(await admin.GetAsync($"{AdminBase}/presets"));

        catalog.EnumerateArray().Select(f => f.GetProperty("key").GetString()).Should().Contain("pt.packages");
        presets.EnumerateArray().Select(p => p.GetProperty("code").GetString())
            .Should().BeEquivalentTo(new[] { "business", "pt_module" });
    }

    [Fact]
    public async Task Pt_module_gym_is_blocked_from_off_features_and_override_unblocks_only_that_gym()
    {
        var admin = CreateAdminClient();
        var ptGym = await RegisterGymAsync();
        var otherGym = await RegisterGymAsync();
        await SetPresetAsync(admin, new PresetAssignment(ptGym, "pt_module"));
        await SetPresetAsync(admin, new PresetAssignment(otherGym, "pt_module"));

        var ptTrainee = TraineeOf(ptGym);
        var otherTrainee = TraineeOf(otherGym);

        // OFF in pt_module → 403 FEATURE_DISABLED:nutrition
        var blocked = await ptTrainee.GetAsync(WaterLogUrl);
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCodeAsync(blocked)).Should().Be("FEATURE_DISABLED:nutrition");

        // Super Admin turns nutrition ON for one gym only.
        var set = await admin.PutAsJsonAsync(
            $"{AdminBase}/gyms/{ptGym}/overrides/nutrition", new { enabled = true, reason = "pilot request" });
        set.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ErrorCodeAsync(await ptTrainee.GetAsync(WaterLogUrl))).Should().NotStartWith("FEATURE_DISABLED");
        (await ErrorCodeAsync(await otherTrainee.GetAsync(WaterLogUrl))).Should().Be("FEATURE_DISABLED:nutrition");

        // Flags endpoint reflects the change for that gym only.
        (await MyFlagAsync(ptTrainee, "nutrition")).Should().BeTrue();
        (await MyFlagAsync(otherTrainee, "nutrition")).Should().BeFalse();

        // Gym detail shows the override source.
        var detail = await ReadDataAsync(await admin.GetAsync($"{AdminBase}/gyms/{ptGym}"));
        var row = detail.GetProperty("flags").EnumerateArray()
            .Single(f => f.GetProperty("feature").GetProperty("key").GetString() == "nutrition");
        row.GetProperty("enabled").GetBoolean().Should().BeTrue();
        row.GetProperty("source").GetString().Should().Be("override");

        // Audit log records who/what/old → new.
        var audit = await ReadDataAsync(await admin.GetAsync($"{AdminBase}/audit?gymId={ptGym}&featureKey=nutrition"));
        var entry = audit.GetProperty("items").EnumerateArray().First();
        entry.GetProperty("changeType").GetString().Should().Be("override_set");
        entry.GetProperty("oldValue").GetString().Should().Be("off (preset)");
        entry.GetProperty("newValue").GetString().Should().Be("on (override)");

        // Reset to preset → blocked again.
        (await admin.DeleteAsync($"{AdminBase}/gyms/{ptGym}/overrides/nutrition")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await ErrorCodeAsync(await ptTrainee.GetAsync(WaterLogUrl))).Should().Be("FEATURE_DISABLED:nutrition");
    }

    [Fact]
    public async Task Pt_features_are_reachable_in_pt_module()
    {
        var admin = CreateAdminClient();
        var gym = await RegisterGymAsync();
        await SetPresetAsync(admin, new PresetAssignment(gym, "pt_module"));

        var response = await TraineeOf(gym).GetAsync("api/v1/billing/me/packages");

        (await ErrorCodeAsync(response)).Should().NotStartWith("FEATURE_DISABLED");
        (await MyFlagAsync(TraineeOf(gym), "pt.packages")).Should().BeTrue();
    }

    [Fact]
    public async Task Switching_a_gym_to_business_turns_every_feature_on()
    {
        var admin = CreateAdminClient();
        var gym = await RegisterGymAsync();
        await SetPresetAsync(admin, new PresetAssignment(gym, "pt_module"));
        var trainee = TraineeOf(gym);
        (await MyFlagAsync(trainee, "classes")).Should().BeFalse();

        await SetPresetAsync(admin, new PresetAssignment(gym, "business"));

        var flags = await ReadDataAsync(await trainee.GetAsync("api/v1/feature-flags/me"));
        flags.GetProperty("preset").GetString().Should().Be("business");
        var values = flags.GetProperty("flags").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetBoolean());
        values.Where(kv => kv.Key != "pt.auto_expiry").Should().OnlyContain(kv => kv.Value);
        values["pt.auto_expiry"].Should().BeFalse("settings keep their default");
    }

    [Fact]
    public async Task Bulk_preset_applies_to_selected_gyms_only()
    {
        var admin = CreateAdminClient();
        var a = await RegisterGymAsync();
        var b = await RegisterGymAsync();
        var c = await RegisterGymAsync();
        foreach (var gym in new[] { a, b, c })
            await SetPresetAsync(admin, new PresetAssignment(gym, "pt_module"));

        var bulk = await admin.PutAsJsonAsync($"{AdminBase}/gyms/preset", new { gymIds = new[] { a, b }, presetCode = "business" });
        (await ReadDataAsync(bulk)).GetProperty("affectedGyms").GetInt32().Should().Be(2);

        (await MyFlagAsync(TraineeOf(a), "classes")).Should().BeTrue();
        (await MyFlagAsync(TraineeOf(b), "classes")).Should().BeTrue();
        (await MyFlagAsync(TraineeOf(c), "classes")).Should().BeFalse();
    }

    [Fact]
    public async Task Unknown_flag_override_is_rejected()
    {
        var admin = CreateAdminClient();
        var gym = await RegisterGymAsync();

        var response = await admin.PutAsJsonAsync($"{AdminBase}/gyms/{gym}/overrides/not.a.flag", new { enabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ------------------------------------------------------------------ helpers

    private HttpClient TraineeOf(Guid gymId) => CreateClient(new TestPrincipal("trainee") { GymId = gymId });

    private async Task<Guid> RegisterGymAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>();
        var id = Guid.NewGuid();
        var suffix = id.ToString("N")[..8];
        registry.Gyms.Add(new GymRegistration
        {
            GymId = id,
            OwnerId = Guid.NewGuid(),
            Name = $"FF Gym {suffix}",
            Slug = $"ff-{suffix}",
            SchemaPrefix = $"g{Random.Shared.Next(100000, 999999)}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        });
        await registry.SaveChangesAsync();
        return id;
    }

    /// <summary>A preset to put a gym on.</summary>
    private sealed record PresetAssignment(Guid GymId, string Preset);

    private static async Task SetPresetAsync(HttpClient admin, PresetAssignment assignment)
    {
        var response = await admin.PutAsJsonAsync(
            $"{AdminBase}/gyms/{assignment.GymId}/preset", new { presetCode = assignment.Preset });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<bool> MyFlagAsync(HttpClient client, string key)
    {
        var data = await ReadDataAsync(await client.GetAsync("api/v1/feature-flags/me"));
        return data.GetProperty("flags").GetProperty(key).GetBoolean();
    }

    private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return JsonDocument.Parse(body).RootElement.GetProperty("data").Clone();
    }

    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;
        try
        {
            var root = JsonDocument.Parse(body).RootElement;
            return root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0
                ? errors[0].GetProperty("code").GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}

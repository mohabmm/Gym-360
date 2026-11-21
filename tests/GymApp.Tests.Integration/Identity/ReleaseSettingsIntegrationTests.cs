using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Identity;

/// <summary>
/// Main Portal → Release Automation: platform admins switch Android auto-publish to
/// Play Internal testing on/off; Codemagic reads the switch with the shared CI token.
/// Auto-publish defaults to ON until an admin turns it off.
/// </summary>
public sealed class ReleaseSettingsIntegrationTests : BaseIntegrationTest
{
    private const string AdminUrl = "api/v1/admin/release-settings";
    private const string CiUrl = "api/v1/ci/release-settings";
    private const string Field = "androidInternalAutoPublish";

    public ReleaseSettingsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private HttpClient CreateCiClient(string? token)
    {
        var client = Factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Add("X-Ci-Token", token);
        return client;
    }

    private static async Task<bool> ReadFlagAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty(Field).GetBoolean();
    }

    private async Task SetFlagAsync(HttpClient admin, bool enabled)
    {
        var response = await admin.PatchAsJsonAsync(AdminUrl, new { androidInternalAutoPublish = enabled });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Admin_toggle_round_trips_and_ci_sees_the_same_value()
    {
        var admin = CreateAdminClient(Guid.NewGuid());
        var ci = CreateCiClient(IntegrationTestWebAppFactory.CiReleaseToken);

        try
        {
            await SetFlagAsync(admin, false);
            Assert.False(await ReadFlagAsync(await admin.GetAsync(AdminUrl)));
            Assert.False(await ReadFlagAsync(await ci.GetAsync(CiUrl)));

            await SetFlagAsync(admin, true);
            Assert.True(await ReadFlagAsync(await admin.GetAsync(AdminUrl)));
            Assert.True(await ReadFlagAsync(await ci.GetAsync(CiUrl)));
        }
        finally
        {
            // Leave the shared database at the default (on) for other tests.
            await SetFlagAsync(admin, true);
        }
    }

    [Fact]
    public async Task Auto_publish_defaults_to_on()
    {
        var ci = CreateCiClient(IntegrationTestWebAppFactory.CiReleaseToken);

        Assert.True(await ReadFlagAsync(await ci.GetAsync(CiUrl)));
    }

    [Fact]
    public async Task Non_admin_cannot_change_the_toggle()
    {
        var owner = CreateGymOwnerClient(Guid.NewGuid(), Guid.NewGuid());

        var response = await owner.PatchAsJsonAsync(AdminUrl, new { androidInternalAutoPublish = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-token")]
    public async Task Ci_endpoint_rejects_missing_or_wrong_token(string? token)
    {
        var response = await CreateCiClient(token).GetAsync(CiUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

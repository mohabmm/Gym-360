using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// GET /api/v1/gyms/{gymId}/members/{memberId}/onboarding — lets the trainer app load a
/// member's saved intake form (it could previously only be written, so reopening the form
/// showed it blank and saving again wiped the earlier answers). The form is health data,
/// so reads are restricted to the member, gym-scoped trainers/staff, and platform admins.
/// </summary>
public sealed class MemberOnboardingIntegrationTests : BaseIntegrationTest
{
    public MemberOnboardingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private readonly Guid _gymId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();

    private string Url => $"api/v1/gyms/{_gymId}/members/{_memberId}/onboarding";

    private async Task SaveAsync(HttpClient client, string goals, string? injuries, string level)
    {
        var response = await client.PostAsJsonAsync(Url, new
        {
            MedicalHistory = "None",
            FitnessGoals = goals,
            Injuries = injuries,
            Medications = (string?)null,
            FitnessLevel = level,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<JsonElement> DataOf(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data");
    }

    [Fact]
    public async Task Get_NothingSavedYet_ReturnsOkWithNullData()
    {
        var response = await CreateTrainerClient(gymId: _gymId).GetAsync(Url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await DataOf(response)).ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_AfterSave_ReturnsTheSavedForm_AndReflectsUpdates()
    {
        var trainer = CreateTrainerClient(gymId: _gymId);
        await SaveAsync(trainer, "Fat loss", "Old knee injury", "intermediate");

        var first = await DataOf(await trainer.GetAsync(Url));
        first.GetProperty("memberId").GetGuid().Should().Be(_memberId);
        first.GetProperty("fitnessGoals").GetString().Should().Be("Fat loss");
        first.GetProperty("injuries").GetString().Should().Be("Old knee injury");
        first.GetProperty("fitnessLevel").GetString().Should().Be("intermediate");
        first.GetProperty("medications").ValueKind.Should().Be(JsonValueKind.Null);

        await SaveAsync(trainer, "Muscle gain", "Old knee injury", "advanced");

        var second = await DataOf(await trainer.GetAsync(Url));
        second.GetProperty("id").GetGuid().Should().Be(first.GetProperty("id").GetGuid());
        second.GetProperty("fitnessGoals").GetString().Should().Be("Muscle gain");
        second.GetProperty("fitnessLevel").GetString().Should().Be("advanced");
    }

    [Fact]
    public async Task Get_AllowedForMemberThemself_GymStaff_AndAdmin()
    {
        await SaveAsync(CreateTrainerClient(gymId: _gymId), "Fat loss", null, "beginner");

        (await CreateTraineeClient(_memberId, _gymId).GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateStaffClient(gymId: _gymId).GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateAdminClient().GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ForbiddenForOtherMembers_AndPrincipalsOfOtherGyms()
    {
        await SaveAsync(CreateTrainerClient(gymId: _gymId), "Fat loss", null, "beginner");
        var otherGym = Guid.NewGuid();

        (await CreateTraineeClient(Guid.NewGuid(), _gymId).GetAsync(Url))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "another member of the same gym");
        (await CreateTrainerClient(gymId: otherGym).GetAsync(Url))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "a trainer scoped to a different gym");
        (await CreateStaffClient(gymId: otherGym).GetAsync(Url))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "staff scoped to a different gym");
        (await CreateTrainerClient().GetAsync(Url))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "a trainer with no gym scope");
    }

    [Fact]
    public async Task Get_Unauthenticated_ReturnsUnauthorized()
    {
        (await HttpClient.GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// Authorization of the <c>/gyms/{gymId}/members…</c> endpoints. They used to accept any
/// authenticated user, so a trainee could list the roster, read and edit other members,
/// freeze them, and read trainers' private notes and intake (health) data.
/// </summary>
public sealed class MemberEndpointsAuthorizationIntegrationTests : BaseIntegrationTest
{
    public MemberEndpointsAuthorizationIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private Guid _gymId;
    private Guid _memberId;
    private Guid _otherMemberId;
    private readonly Guid _otherGymId = Guid.NewGuid();

    public override async Task InitializeAsync()
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = $"Authz Test Gym {Guid.NewGuid():N}",
                Description = null,
                Address = new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
                ContactInfo = new ContactInfo("gym@test.com", "+201234567890"),
                GymCode = GymCode.Create($"AUZ{Guid.NewGuid().ToString("N")[..3]}")
            });
        gym.Approve();
        _memberId = Guid.NewGuid();
        _otherMemberId = Guid.NewGuid();
        gym.AddMember(new MemberId(_memberId));
        gym.AddMember(new MemberId(_otherMemberId));
        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();
        _gymId = gym.Id;
    }

    private string Members => $"api/v1/gyms/{_gymId}/members";
    private string Member => $"{Members}/{_memberId}";

    // Principals
    private HttpClient Staff => CreateStaffClient(gymId: _gymId);
    private HttpClient Owner => CreateGymOwnerClient(gymId: _gymId);
    private HttpClient OwnerWithoutSelectedGym => CreateGymOwnerClient();
    private HttpClient Trainer => CreateTrainerClient(gymId: _gymId);
    private HttpClient Self => CreateTraineeClient(_memberId, _gymId);
    private HttpClient OtherTrainee => CreateTraineeClient(_otherMemberId, _gymId);
    private HttpClient StaffOfOtherGym => CreateStaffClient(gymId: _otherGymId);
    private HttpClient TrainerOfOtherGym => CreateTrainerClient(gymId: _otherGymId);
    private HttpClient TrainerWithoutGym => CreateTrainerClient();
    private HttpClient Admin => CreateAdminClient();

    private static async Task<HttpStatusCode> Status(Task<HttpResponseMessage> call) => (await call).StatusCode;

    private static Task<HttpResponseMessage> Post(HttpClient client, string url, object? body = null) =>
        client.PostAsJsonAsync(url, body ?? new { });

    // --- Roster -----------------------------------------------------------------

    [Fact]
    public async Task Roster_AllowedForStaffOwnerTrainerAndAdmin()
    {
        var url = $"{Members}?page=1&pageSize=20";

        (await Status(Staff.GetAsync(url))).Should().Be(HttpStatusCode.OK);
        (await Status(Owner.GetAsync(url))).Should().Be(HttpStatusCode.OK);
        (await Status(OwnerWithoutSelectedGym.GetAsync(url))).Should().Be(HttpStatusCode.OK);
        (await Status(Trainer.GetAsync(url))).Should().Be(HttpStatusCode.OK);
        (await Status(Admin.GetAsync(url))).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Roster_ForbiddenForTrainees_AndPrincipalsOfOtherGyms()
    {
        var url = $"{Members}?page=1&pageSize=20";

        (await Status(Self.GetAsync(url))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(StaffOfOtherGym.GetAsync(url))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(TrainerOfOtherGym.GetAsync(url))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(TrainerWithoutGym.GetAsync(url))).Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Roster_ForTrainer_IsPinnedToTheirOwnClients()
    {
        // This trainer has no clients. Without pinning, omitting trainerId (or passing
        // someone else's) returned the whole gym roster.
        foreach (var url in new[]
                 {
                     $"{Members}?page=1&pageSize=20",
                     $"{Members}?page=1&pageSize=20&trainerId={Guid.NewGuid()}",
                 })
        {
            var response = await Trainer.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            data.GetProperty("totalCount").GetInt32().Should().Be(0);
            data.GetProperty("items").GetArrayLength().Should().Be(0);
        }

        var staffView = (await (await Staff.GetAsync($"{Members}?page=1&pageSize=20"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        staffView.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    // --- Single member ------------------------------------------------------------

    [Fact]
    public async Task GetMember_AllowedForStaffTrainerAndTheMember_ForbiddenForOtherTrainees()
    {
        (await Status(Staff.GetAsync(Member))).Should().Be(HttpStatusCode.OK);
        (await Status(Trainer.GetAsync(Member))).Should().Be(HttpStatusCode.OK);
        (await Status(Self.GetAsync(Member))).Should().Be(HttpStatusCode.OK);

        (await Status(OtherTrainee.GetAsync(Member))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(TrainerOfOtherGym.GetAsync(Member))).Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Staff-only mutations -----------------------------------------------------

    [Fact]
    public async Task StaffOnlyMutations_ForbiddenForTrainersAndTrainees()
    {
        foreach (var client in new[] { Trainer, Self, OtherTrainee, StaffOfOtherGym })
        {
            (await Status(Post(client, Members, new { UserId = Guid.NewGuid() })))
                .Should().Be(HttpStatusCode.Forbidden, "add member");
            (await Status(client.PutAsJsonAsync(Member, new { Name = "Hacked Name" })))
                .Should().Be(HttpStatusCode.Forbidden, "update member");
            (await Status(Post(client, $"{Member}/freeze")))
                .Should().Be(HttpStatusCode.Forbidden, "freeze member");
            (await Status(Post(client, $"{Member}/unfreeze")))
                .Should().Be(HttpStatusCode.Forbidden, "unfreeze member");
            (await Status(client.DeleteAsync(Member)))
                .Should().Be(HttpStatusCode.Forbidden, "remove member");
        }
    }

    [Fact]
    public async Task StaffOnlyMutations_ReachTheHandlerForStaff()
    {
        // Past the authorization filter; the handler's own outcome (e.g. 404 for a member
        // without a directory row in this bare fixture) is not what is under test here.
        (await Status(Post(Staff, $"{Member}/freeze"))).Should().NotBe(HttpStatusCode.Forbidden);
        (await Status(Post(Staff, $"{Member}/unfreeze"))).Should().NotBe(HttpStatusCode.Forbidden);
        (await Status(Post(Staff, Members, new { UserId = Guid.NewGuid() })))
            .Should().NotBe(HttpStatusCode.Forbidden);
        (await Status(Staff.PutAsJsonAsync(Member, new { Section = "Mixed" })))
            .Should().NotBe(HttpStatusCode.Forbidden);
    }

    // --- Notes (private trainer/staff notes) --------------------------------------

    [Fact]
    public async Task Notes_AllowedForTrainerAndStaff_HiddenFromMembers()
    {
        var notes = $"{Member}/notes";

        (await Status(Post(Trainer, notes, new { Content = "Knee sensitivity" })))
            .Should().Be(HttpStatusCode.Created);
        (await Status(Trainer.GetAsync(notes))).Should().Be(HttpStatusCode.OK);
        (await Status(Staff.GetAsync(notes))).Should().Be(HttpStatusCode.OK);

        (await Status(Self.GetAsync(notes)))
            .Should().Be(HttpStatusCode.Forbidden, "notes are private to trainers and staff");
        (await Status(OtherTrainee.GetAsync(notes))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(Post(OtherTrainee, notes, new { Content = "x" }))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(TrainerOfOtherGym.GetAsync(notes))).Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Intake form --------------------------------------------------------------

    [Fact]
    public async Task SaveOnboarding_AllowedForTrainerAndTheMember_ForbiddenForOthers()
    {
        var url = $"{Member}/onboarding";
        var form = new { FitnessGoals = "Fat loss", FitnessLevel = "beginner" };

        (await Status(Post(Trainer, url, form))).Should().Be(HttpStatusCode.Created);
        (await Status(Post(Self, url, form))).Should().Be(HttpStatusCode.Created);

        (await Status(Post(OtherTrainee, url, form))).Should().Be(HttpStatusCode.Forbidden);
        (await Status(Post(TrainerOfOtherGym, url, form))).Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Unauthenticated ----------------------------------------------------------

    [Fact]
    public async Task Unauthenticated_ReturnsUnauthorized()
    {
        (await Status(HttpClient.GetAsync($"{Members}?page=1&pageSize=20"))).Should().Be(HttpStatusCode.Unauthorized);
        (await Status(HttpClient.GetAsync(Member))).Should().Be(HttpStatusCode.Unauthorized);
        (await Status(HttpClient.GetAsync($"{Member}/notes"))).Should().Be(HttpStatusCode.Unauthorized);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// The staff app is the controller: a new member has no sessions until the desk assigns a coach
/// and any number of free sessions; then the member sees exactly that package.
/// </summary>
public sealed class StaffAssignsFreeSessionsIntegrationTests : BaseIntegrationTest
{
    private const string QuickAssignUrl = "api/v1/billing/packages/quick-assign";
    private const string MyPackagesUrl = "api/v1/billing/me/packages";

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();

    public StaffAssignsFreeSessionsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Member_has_no_sessions_until_desk_assigns_a_coach_and_free_sessions()
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = CreateStaffClient(_deskId, gymId);
        var trainee = CreateTraineeClient(_traineeId, gymId);
        desk.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());
        trainee.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());

        (await PackagesAsync(trainee)).Should().BeEmpty("nothing is assigned yet");

        var response = await desk.PostAsJsonAsync(QuickAssignUrl, Assign(gymId, sessions: 7));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var package = (await PackagesAsync(trainee)).Should().ContainSingle().Subject;
        package.GetProperty("trainerId").GetGuid().Should().Be(_coachId);
        package.GetProperty("totalSessions").GetInt32().Should().Be(7);
        package.GetProperty("remainingSessions").GetInt32().Should().Be(7);
        package.GetProperty("status").GetString().Should().Be("Active");
        package.GetProperty("name").GetString().Should().Be("Free: 7 sessions");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Session_count_outside_one_to_one_hundred_is_rejected(int sessions)
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = CreateStaffClient(_deskId, gymId);
        desk.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());

        var response = await desk.PostAsJsonAsync(QuickAssignUrl, Assign(gymId, sessions));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private object Assign(Guid gymId, int sessions) => new
    {
        gymId,
        trainerId = _coachId,
        memberId = _traineeId,
        sessionCount = sessions,
        assignedBy = _deskId,
    };

    private static async Task<List<JsonElement>> PackagesAsync(HttpClient trainee)
    {
        var data = await PtTestGym.DataAsync(trainee, MyPackagesUrl);
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().ToList();
    }
}

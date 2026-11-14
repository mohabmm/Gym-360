using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// GET /billing/packages used to accept any signed-in user, so anyone could read any gym's
/// packages. Now: staff of the gym see all, a trainer of the gym sees only packages they
/// coach, everyone else gets 403.
/// </summary>
public sealed class PackageListAuthorizationIntegrationTests : BaseIntegrationTest
{
    private Guid _gymId;
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _otherTraineeId = Guid.NewGuid();
    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _otherCoachId = Guid.NewGuid();

    public PackageListAuthorizationIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    public override async Task InitializeAsync() =>
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);

    private HttpClient WithGymHeader(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private Task<HttpResponseMessage> ListAsync(HttpClient client) => client.GetAsync("api/v1/billing/packages");

    private async Task AssignAsync(HttpClient desk, Guid coachId, Guid memberId) =>
        (await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId,
            trainerId = coachId,
            memberId,
            sessionCount = 4,
            assignedBy = Guid.NewGuid(),
        })).EnsureSuccessStatusCode();

    [Fact]
    public async Task Staff_of_the_gym_see_every_package()
    {
        var desk = WithGymHeader(CreateStaffClient(gymId: _gymId));
        await AssignAsync(desk, _coachId, _traineeId);
        await AssignAsync(desk, _otherCoachId, _otherTraineeId);

        var data = await PtTestGym.DataAsync(desk, "api/v1/billing/packages");

        data.EnumerateArray().Select(p => p.GetProperty("trainerId").GetGuid())
            .Should().Contain(new[] { _coachId, _otherCoachId });
    }

    [Fact]
    public async Task A_trainer_sees_only_the_packages_they_coach()
    {
        var desk = WithGymHeader(CreateStaffClient(gymId: _gymId));
        await AssignAsync(desk, _coachId, _traineeId);
        await AssignAsync(desk, _otherCoachId, _otherTraineeId);
        var coach = WithGymHeader(CreateTrainerClient(_coachId, _gymId));

        var data = await PtTestGym.DataAsync(coach, $"api/v1/billing/packages?trainerId={_otherCoachId}");

        data.EnumerateArray().Should().NotBeEmpty();
        data.EnumerateArray().Select(p => p.GetProperty("trainerId").GetGuid()).Should().OnlyContain(id => id == _coachId);
    }

    [Fact]
    public async Task A_trainee_is_forbidden()
    {
        var response = await ListAsync(WithGymHeader(CreateTraineeClient(_traineeId, _gymId)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_of_another_gym_are_forbidden()
    {
        var response = await ListAsync(WithGymHeader(CreateStaffClient(gymId: Guid.NewGuid())));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
}

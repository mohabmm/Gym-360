using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;

namespace GymApp.Tests.Integration.Billing;

/// <summary>
/// Staff app "Assign Package": the package is recorded as assigned by the signed-in staff member
/// (never an id sent in the body), and a member keeps one coach at a time — renewing with the same
/// coach works, a different coach waits until the sessions are used.
/// </summary>
public sealed class StaffAssignPackageIntegrationTests : BaseIntegrationTest
{
    private const string AssignUrl = "api/v1/billing/packages/assign";
    private const string TemplatesUrl = "api/v1/billing/package-templates";

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();

    public StaffAssignPackageIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Assigned_by_is_the_signed_in_staff_member_even_if_the_body_claims_someone_else()
    {
        var (desk, gymId, templateId) = await ArrangeAsync();

        var response = await desk.PostAsJsonAsync(AssignUrl, new
        {
            gymId,
            trainerId = _coachId,
            memberId = _traineeId,
            templateId,
            assignedBy = Guid.NewGuid(), // ignored
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var package = (await MemberPackagesAsync(desk, gymId)).Should().ContainSingle().Subject;
        package.GetProperty("assignedBy").GetGuid().Should().Be(_deskId);
        package.GetProperty("totalSessions").GetInt32().Should().Be(12);
    }

    [Fact]
    public async Task Renewal_with_the_same_coach_works_while_sessions_are_left()
    {
        var (desk, gymId, templateId) = await ArrangeAsync();

        (await AssignAsync(desk, gymId, templateId, _coachId)).StatusCode.Should().Be(HttpStatusCode.Created);
        var renewal = await AssignAsync(desk, gymId, templateId, _coachId);

        renewal.StatusCode.Should().Be(HttpStatusCode.Created, await renewal.Content.ReadAsStringAsync());
        (await MemberPackagesAsync(desk, gymId)).Should().HaveCount(2);
    }

    [Fact]
    public async Task A_different_coach_is_refused_while_the_member_has_sessions_left()
    {
        var (desk, gymId, templateId) = await ArrangeAsync();
        (await AssignAsync(desk, gymId, templateId, _coachId)).StatusCode.Should().Be(HttpStatusCode.Created);

        var other = await AssignAsync(desk, gymId, templateId, Guid.NewGuid());

        other.IsSuccessStatusCode.Should().BeFalse();
        (await other.Content.ReadAsStringAsync()).Should().Contain("Billing.Coach.AlreadyExists");
        (await MemberPackagesAsync(desk, gymId)).Should().ContainSingle();
    }

    private async Task<(HttpClient Desk, Guid GymId, Guid TemplateId)> ArrangeAsync()
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = CreateStaffClient(_deskId, gymId);
        desk.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());

        var created = await desk.PostAsJsonAsync(TemplatesUrl, new
        {
            gymId,
            name = "12 sessions",
            sessionCount = 12,
            pricePerSession = 150m,
        });
        var body = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var templateId = JsonDocument.Parse(body).RootElement.GetProperty("data").GetGuid();
        return (desk, gymId, templateId);
    }

    private Task<HttpResponseMessage> AssignAsync(HttpClient desk, Guid gymId, Guid templateId, Guid trainerId) =>
        desk.PostAsJsonAsync(AssignUrl, new { gymId, trainerId, memberId = _traineeId, templateId });

    private async Task<List<JsonElement>> MemberPackagesAsync(HttpClient desk, Guid gymId)
    {
        var data = await PtTestGym.DataAsync(desk, $"api/v1/billing/packages?gymId={gymId}&memberId={_traineeId}");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().ToList();
    }
}

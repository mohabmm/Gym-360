using System.Net;
using System.Net.Http.Json;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// Authorization of <c>POST /gyms/{gymId}/trainers…</c>. These used to accept any signed-in
/// user, so anyone (even a trainee of another gym) could add, approve or reject trainers.
/// Add: staff of the gym. Approve/reject: owner or manager of the gym.
/// </summary>
public sealed class TrainerRosterAuthorizationIntegrationTests : BaseIntegrationTest
{
    public TrainerRosterAuthorizationIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private Guid _gymId;
    private readonly Guid _trainerId = Guid.NewGuid();

    public override async Task InitializeAsync()
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = $"Trainer Authz Gym {Guid.NewGuid():N}",
                Description = null,
                Address = new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
                ContactInfo = new ContactInfo("gym@test.com", "+201234567890"),
                GymCode = GymCode.Create($"TRZ{Guid.NewGuid().ToString("N")[..3]}")
            });
        gym.Approve();
        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();
        _gymId = gym.Id;
    }

    private HttpClient StaffWithSubRole(string subRole, Guid? gymId = null) =>
        CreateAuthenticatedClient(new TestPrincipal("gym_staff")
        {
            GymId = gymId ?? _gymId, StaffRole = "staff", StaffSubRole = subRole
        });

    private Task<HttpResponseMessage> AddTrainer(HttpClient client) =>
        client.PostAsJsonAsync($"api/v1/gyms/{_gymId}/trainers", new { trainerId = _trainerId });

    private Task<HttpResponseMessage> Approve(HttpClient client) =>
        client.PostAsync($"api/v1/gyms/{_gymId}/trainers/{_trainerId}/approve", null);

    private Task<HttpResponseMessage> Reject(HttpClient client) =>
        client.PostAsJsonAsync($"api/v1/gyms/{_gymId}/trainers/{_trainerId}/reject", new { reason = "no" });

    [Theory]
    [InlineData("Receptionist")]
    [InlineData("Manager")]
    public async Task AddTrainer_AsStaffOfThisGym_PassesAuthorization(string subRole)
    {
        var response = await AddTrainer(StaffWithSubRole(subRole));

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddTrainer_AsOwner_PassesAuthorization()
    {
        var response = await AddTrainer(CreateGymOwnerClient(gymId: _gymId));

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddTrainer_AsTraineeOfThisGym_ReturnsForbidden()
    {
        var response = await AddTrainer(CreateTraineeClient(gymId: _gymId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddTrainer_AsTrainerOfThisGym_ReturnsForbidden()
    {
        var response = await AddTrainer(CreateTrainerClient(gymId: _gymId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddTrainer_AsStaffOfAnotherGym_ReturnsForbidden()
    {
        var response = await AddTrainer(StaffWithSubRole("Manager", Guid.NewGuid()));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApproveAndReject_AsManager_PassAuthorization()
    {
        (await Approve(StaffWithSubRole("Manager"))).StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        (await Reject(StaffWithSubRole("Manager"))).StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveAndReject_AsOwner_PassAuthorization()
    {
        var owner = CreateGymOwnerClient(gymId: _gymId);

        (await Approve(owner)).StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        (await Reject(owner)).StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveAndReject_AsReceptionist_ReturnForbidden()
    {
        (await Approve(StaffWithSubRole("Receptionist"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Reject(StaffWithSubRole("Receptionist"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveAndReject_AsTrainee_ReturnForbidden()
    {
        var trainee = CreateTraineeClient(gymId: _gymId);

        (await Approve(trainee)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Reject(trainee)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

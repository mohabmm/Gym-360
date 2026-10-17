using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gyms;

public sealed class KioskPinIntegrationTests : BaseIntegrationTest
{
    public KioskPinIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private async Task<Gym> SeedGymAsync()
    {
        var repository = GetService<IGymRepository>();
        var unitOfWork = GetService<IGymsUnitOfWork>();

        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = $"Kiosk Test Gym {Guid.NewGuid():N}",
                Description = "Fixture gym for kiosk PIN tests",
                Address = new Address("1 Test St", "Cairo", "Cairo", "EG", "11511", 30.04, 31.24),
                ContactInfo = new ContactInfo("kiosk@test.local", "+201000000000", null),
                GymCode = GymCode.Generate()
            });

        await repository.AddAsync(gym);
        await unitOfWork.SaveChangesAsync();
        return gym;
    }

    // The gym_owner_or_manager policy gates Manager via the `staffSubRole` JWT
    // claim (IStaffSubRoleResolver), not the bare `staffRole` claim — the helper
    // must mint staffSubRole so Manager is authorized (Owner passes via its own
    // staffRole=owner branch).
    private HttpClient ManagerClient(Guid gymId) =>
        CreateAuthenticatedClient(new TestPrincipal("gym_staff")
        {
            GymId = gymId, StaffRole = "manager", StaffSubRole = "Manager"
        });

    private HttpClient ReceptionistClient(Guid gymId) =>
        CreateAuthenticatedClient(new TestPrincipal("gym_staff")
        {
            GymId = gymId, StaffRole = "receptionist", StaffSubRole = "Receptionist"
        });

    private HttpClient OwnerClient(Guid gymId, Guid? ownerId = null) =>
        CreateGymOwnerClient(userId: ownerId, gymId: gymId);

    private HttpClient StaffClient(Guid gymId) =>
        CreateStaffClient(gymId: gymId);

    private sealed record InfoEnvelope(KioskPinInfoResponse Data);
    private sealed record VerifyPayload(bool Verified);
    private sealed record VerifyEnvelope(VerifyPayload Data);

    [Fact]
    public async Task GetInfo_FreshGym_ReturnsIsSetFalse()
    {
        var gym = await SeedGymAsync();
        var client = StaffClient(gym.Id);

        var response = await client.GetAsync($"api/v1/gyms/{gym.Id}/kiosk-pin");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InfoEnvelope>();
        body!.Data.IsSet.Should().BeFalse();
        body.Data.Hash.Should().BeNull();
    }

    [Fact]
    public async Task SetPin_FirstTime_NoCurrentRequired_Succeeds()
    {
        var gym = await SeedGymAsync();
        var owner = OwnerClient(gym.Id);

        var set = await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "5678", CurrentPin = (string?)null });
        set.StatusCode.Should().Be(HttpStatusCode.OK);

        var info = await owner.GetAsync($"api/v1/gyms/{gym.Id}/kiosk-pin");
        var body = await info.Content.ReadFromJsonAsync<InfoEnvelope>();
        body!.Data.IsSet.Should().BeTrue();
        body.Data.Hash.Should().NotBeNullOrEmpty();
        body.Data.Hash!.Should().StartWith("$2"); // BCrypt hash prefix
    }

    [Fact]
    public async Task Verify_CorrectPin_ReturnsVerifiedTrue_WrongPin_ReturnsFalse()
    {
        var gym = await SeedGymAsync();
        var owner = OwnerClient(gym.Id);
        await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "5678", CurrentPin = (string?)null });

        var staff = StaffClient(gym.Id);

        var good = await staff.PostAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin/verify",
            new { Pin = "5678" });
        good.StatusCode.Should().Be(HttpStatusCode.OK);
        (await good.Content.ReadFromJsonAsync<VerifyEnvelope>())!.Data.Verified.Should().BeTrue();

        var bad = await staff.PostAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin/verify",
            new { Pin = "9999" });
        bad.StatusCode.Should().Be(HttpStatusCode.OK);
        (await bad.Content.ReadFromJsonAsync<VerifyEnvelope>())!.Data.Verified.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePin_WithoutCurrent_Fails_WithCurrent_Succeeds()
    {
        var gym = await SeedGymAsync();
        var owner = OwnerClient(gym.Id);
        await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "5678", CurrentPin = (string?)null });

        var missingCurrent = await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "4321", CurrentPin = (string?)null });
        missingCurrent.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var wrongCurrent = await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "4321", CurrentPin = "0000" });
        wrongCurrent.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var correct = await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "4321", CurrentPin = "5678" });
        correct.StatusCode.Should().Be(HttpStatusCode.OK);

        var staff = StaffClient(gym.Id);
        var old = await staff.PostAsJsonAsync($"api/v1/gyms/{gym.Id}/kiosk-pin/verify", new { Pin = "5678" });
        (await old.Content.ReadFromJsonAsync<VerifyEnvelope>())!.Data.Verified.Should().BeFalse();

        var fresh = await staff.PostAsJsonAsync($"api/v1/gyms/{gym.Id}/kiosk-pin/verify", new { Pin = "4321" });
        (await fresh.Content.ReadFromJsonAsync<VerifyEnvelope>())!.Data.Verified.Should().BeTrue();
    }

    [Fact]
    public async Task ClearPin_WithCurrent_RemovesLock()
    {
        var gym = await SeedGymAsync();
        var owner = OwnerClient(gym.Id);
        await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "5678", CurrentPin = (string?)null });

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/gyms/{gym.Id}/kiosk-pin")
        {
            Content = JsonContent.Create(new { CurrentPin = "5678" })
        };
        var clear = await owner.SendAsync(req);
        clear.StatusCode.Should().Be(HttpStatusCode.OK);

        var info = await owner.GetAsync($"api/v1/gyms/{gym.Id}/kiosk-pin");
        (await info.Content.ReadFromJsonAsync<InfoEnvelope>())!.Data.IsSet.Should().BeFalse();

        // No PIN → verify returns true (no lock).
        var staff = StaffClient(gym.Id);
        var verify = await staff.PostAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin/verify",
            new { Pin = "0000" });
        (await verify.Content.ReadFromJsonAsync<VerifyEnvelope>())!.Data.Verified.Should().BeTrue();
    }

    [Fact]
    public async Task SetPin_Authorization_OwnerAndManagerAllowed_ReceptionistForbidden()
    {
        var gym = await SeedGymAsync();

        var owner = OwnerClient(gym.Id);
        var ownerSet = await owner.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "1111", CurrentPin = (string?)null });
        ownerSet.StatusCode.Should().Be(HttpStatusCode.OK);

        var manager = ManagerClient(gym.Id);
        var managerSet = await manager.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "2222", CurrentPin = "1111" });
        managerSet.StatusCode.Should().Be(HttpStatusCode.OK);

        var receptionist = ReceptionistClient(gym.Id);
        var receptionistSet = await receptionist.PutAsJsonAsync(
            $"api/v1/gyms/{gym.Id}/kiosk-pin",
            new { NewPin = "3333", CurrentPin = "2222" });
        receptionistSet.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

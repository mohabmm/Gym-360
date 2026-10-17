using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// PUT /api/v1/gyms/{gymId} — "Change gym settings" is Owner + Manager in the
/// staff permission matrix (docs/01), plus platform admins. Receptionists and
/// non-staff must be rejected before the handler runs.
/// </summary>
public sealed class UpdateGymAuthorizationIntegrationTests : BaseIntegrationTest
{
    public UpdateGymAuthorizationIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    private static object Body => new { amenities = new[] { "Sauna" } };

    [Fact]
    public async Task UpdateGym_AsManager_PassesAuthorization()
    {
        var gymId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(new TestPrincipal("gym_staff")
        {
            GymId = gymId, StaffRole = "staff", StaffSubRole = "Manager"
        });

        var response = await client.PutAsJsonAsync($"api/v1/gyms/{gymId}", Body);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateGym_AsOwner_PassesAuthorization()
    {
        var gymId = Guid.NewGuid();
        var client = CreateGymOwnerClient(gymId: gymId);

        var response = await client.PutAsJsonAsync($"api/v1/gyms/{gymId}", Body);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateGym_AsReceptionist_ReturnsForbidden()
    {
        var gymId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(new TestPrincipal("gym_staff")
        {
            GymId = gymId, StaffRole = "staff", StaffSubRole = "Receptionist"
        });

        var response = await client.PutAsJsonAsync($"api/v1/gyms/{gymId}", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateGym_AsTrainee_ReturnsForbidden()
    {
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(gymId: gymId);

        var response = await client.PutAsJsonAsync($"api/v1/gyms/{gymId}", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Gyms;

public sealed class GymsFunctionalTests : BaseFunctionalTest
{
    public GymsFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    private static string AdminGymUrl(string action) => $"api/v1/admin/gyms/{Guid.NewGuid()}/{action}";

    private Task<HttpResponseMessage> PostCheckInAsStaffAsync(object body)
    {
        var gymId = Guid.NewGuid();
        return CreateStaffClient(gymId: gymId).PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", body);
    }

    private Task<HttpResponseMessage> PostTrainerAsOwnerAsync(object body)
    {
        var gymId = Guid.NewGuid();
        return CreateOwnerClient(gymId: gymId).PostAsJsonAsync($"api/v1/gyms/{gymId}/trainers", body);
    }

    #region POST /api/v1/gyms

    [Fact]
    public async Task CreateGym_ValidData_Returns201()
    {
        // Arrange — POST /api/v1/gyms is platform_admin-only (RegisterGymEndpoint).
        // The gym's OwnerId is carried in the body, distinct from the admin caller.
        var ownerId = Guid.NewGuid();
        var client = CreateAdminClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gyms", new
        {
            OwnerId = ownerId,
            Name = $"Gym-{Guid.NewGuid():N}",
            Description = "Test gym",
            Address = new { Street = "123 Test", City = "Cairo", State = "Cairo", Country = "Egypt", ZipCode = "11511" },
            Contact = new { Phone = "+201111222333", Email = "gym@test.com" }
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateGym_MissingName_Returns400()
    {
        // Arrange — POST /api/v1/gyms is platform_admin-only (RegisterGymEndpoint).
        var client = CreateAdminClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gyms", new
        {
            OwnerId = Guid.NewGuid(),
            Name = "",
            Address = new { Street = "123 Test", City = "Cairo", Governorate = "Cairo", PostalCode = "11511" },
            Contact = new { Phone = "+201111222333", Email = "gym@test.com" }
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGym_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/gyms", new
        {
            OwnerId = Guid.NewGuid(),
            Name = "Unauthorized Gym",
            Address = new { Street = "123 Test", City = "Cairo", Governorate = "Cairo", PostalCode = "11511" },
            Contact = new { Phone = "+201111222333", Email = "gym@test.com" }
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/gyms/{id}

    [Fact]
    public async Task GetGym_ValidId_ReturnsNotFoundForNonExistent()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync($"api/v1/gyms/{Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGym_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync($"api/v1/gyms/{Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/gyms/{id}/check-in

    [Fact]
    public async Task CheckIn_ValidQr_ProcessesRequest() =>
        AssertAuthenticated(await PostCheckInAsStaffAsync(new
        {
            Method = "qr",
            QrToken = $"qr-{Guid.NewGuid()}",
            MemberId = Guid.NewGuid(),
            StaffId = Guid.NewGuid()
        }));

    [Fact]
    public async Task CheckIn_ValidMemberNumber_ProcessesRequest() =>
        AssertAuthenticated(await PostCheckInAsStaffAsync(new
        {
            Method = "member_number",
            MemberNumber = "MEM001",
            StaffId = Guid.NewGuid()
        }));

    [Fact]
    public async Task CheckIn_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/check-in",
            new { Method = "qr", QrToken = "test" });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/gyms/{id}/trainers (Bio + Specializations)

    [Fact]
    public async Task AddTrainer_WithBioAndSpecializations_AcceptsPayload()
    {
        // Act — endpoint accepts optional Bio and Specializations.
        // The gym is unlikely to exist in the test DB, so 404 from handler is the
        // expected status; what matters is the payload schema is bound (no 400).
        var response = await PostTrainerAsOwnerAsync(new
        {
            TrainerId = Guid.NewGuid(),
            Bio = "Strength coach with 10 years of experience.",
            Specializations = new[] { "Strength", "HIIT" }
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddTrainer_BioTooLong_Returns400() =>
        AssertStatusCode(
            await PostTrainerAsOwnerAsync(new { TrainerId = Guid.NewGuid(), Bio = new string('x', 2001) }),
            HttpStatusCode.BadRequest);

    [Fact]
    public async Task AddTrainer_SpecializationTooLong_Returns400() =>
        AssertStatusCode(
            await PostTrainerAsOwnerAsync(new
            {
                TrainerId = Guid.NewGuid(),
                Specializations = new[] { new string('y', 101) }
            }),
            HttpStatusCode.BadRequest);

    #endregion

    #region POST /api/v1/admin/gyms/{id}/suspend

    [Fact]
    public async Task SuspendGym_AsAdmin_ProcessesRequest() =>
        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(
            AdminGymUrl("suspend"), new { Reason = "Non-payment" }));

    [Fact]
    public async Task SuspendGym_AsTrainee_Returns403() =>
        AssertStatusCode(
            await CreateTraineeClient().PostAsJsonAsync(AdminGymUrl("suspend"), new { Reason = "Attempt" }),
            HttpStatusCode.Forbidden);

    [Fact]
    public async Task SuspendGym_NoToken_Returns401() =>
        AssertStatusCode(
            await AnonymousClient.PostAsJsonAsync(AdminGymUrl("suspend"), new { Reason = "Test" }),
            HttpStatusCode.Unauthorized);

    #endregion

    #region POST /api/v1/admin/gyms/{id}/reactivate

    [Fact]
    public async Task ReactivateGym_AsAdmin_ProcessesRequest() =>
        AssertAuthorized(await CreateAdminClient().PostAsJsonAsync(AdminGymUrl("reactivate"), new { }));

    [Fact]
    public async Task ReactivateGym_AsTrainee_Returns403() =>
        AssertStatusCode(
            await CreateTraineeClient().PostAsJsonAsync(AdminGymUrl("reactivate"), new { }),
            HttpStatusCode.Forbidden);

    #endregion
}

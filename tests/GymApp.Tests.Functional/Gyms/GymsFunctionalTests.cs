using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Gyms;

public sealed class GymsFunctionalTests : BaseFunctionalTest
{
    public GymsFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region POST /api/v1/gyms

    [Fact]
    public async Task CreateGym_ValidData_Returns201()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var client = CreateOwnerClient(ownerId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gyms", new
        {
            OwnerId = ownerId,
            Name = $"Gym-{Guid.NewGuid():N}",
            Description = "Test gym",
            Address = new { Street = "123 Test", City = "Cairo", Governorate = "Cairo", PostalCode = "11511" },
            Contact = new { Phone = "+201111222333", Email = "gym@test.com" }
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateGym_MissingName_Returns400()
    {
        // Arrange
        var client = CreateOwnerClient();

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
    public async Task CheckIn_ValidQr_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", new
        {
            Method = "qr",
            QrToken = $"qr-{Guid.NewGuid()}",
            MemberId = Guid.NewGuid(),
            StaffId = Guid.NewGuid()
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckIn_ValidMemberNumber_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateStaffClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/check-in", new
        {
            Method = "member_number",
            MemberNumber = "MEM001",
            StaffId = Guid.NewGuid()
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

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
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateOwnerClient(gymId: gymId);

        // Act — endpoint accepts optional Bio and Specializations.
        // The gym is unlikely to exist in the test DB, so 404 from handler is the
        // expected status; what matters is the payload schema is bound (no 400).
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/trainers", new
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
    public async Task AddTrainer_BioTooLong_Returns400()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateOwnerClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/trainers", new
        {
            TrainerId = Guid.NewGuid(),
            Bio = new string('x', 2001)
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTrainer_SpecializationTooLong_Returns400()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateOwnerClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync($"api/v1/gyms/{gymId}/trainers", new
        {
            TrainerId = Guid.NewGuid(),
            Specializations = new[] { new string('y', 101) }
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/v1/admin/gyms/{id}/suspend

    [Fact]
    public async Task SuspendGym_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/gyms/{Guid.NewGuid()}/suspend",
            new { Reason = "Non-payment" });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuspendGym_AsTrainee_Returns403()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/gyms/{Guid.NewGuid()}/suspend",
            new { Reason = "Attempt" });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuspendGym_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            $"api/v1/admin/gyms/{Guid.NewGuid()}/suspend",
            new { Reason = "Test" });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/admin/gyms/{id}/reactivate

    [Fact]
    public async Task ReactivateGym_AsAdmin_ProcessesRequest()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/gyms/{Guid.NewGuid()}/reactivate", new { });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReactivateGym_AsTrainee_Returns403()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/admin/gyms/{Guid.NewGuid()}/reactivate", new { });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Forbidden);
    }

    #endregion
}

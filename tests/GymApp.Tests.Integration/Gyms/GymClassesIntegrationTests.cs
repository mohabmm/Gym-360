using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// Integration tests for gym class endpoints added to satisfy the Flutter
/// class-detail/waitlist/attendance and class-delete flows:
///   GET    /api/v1/gyms/{gymId}/classes/{classId}
///   DELETE /api/v1/gyms/{gymId}/classes/{classId}
///   GET    /api/v1/gyms/{gymId}/classes/{classId}/waitlist
///   GET    /api/v1/gyms/{gymId}/classes/{classId}/attendance
/// </summary>
public sealed class GymClassesIntegrationTests : BaseIntegrationTest
{
    public GymClassesIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task<Gym> SeedActiveGym()
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            ownerId: Guid.NewGuid(),
            name: $"Class Test Gym {Guid.NewGuid():N}",
            description: null,
            address: new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
            contactInfo: new ContactInfo("gym@test.com", "+201234567890"),
            gymCode: GymCode.Create($"CLS{Guid.NewGuid().ToString("N")[..3]}"));
        gym.Approve();
        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();
        return gym;
    }

    private async Task<GymClass> SeedActiveClass(Guid gymId, int capacity = 2, bool waitlistEnabled = true)
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gymClass = GymClass.Create(
            gymId: gymId,
            name: "Morning Yoga",
            description: "Gentle flow",
            trainerId: Guid.NewGuid(),
            schedule: new ClassSchedule(
                DayOfWeek.Monday,
                new TimeOnly(8, 0),
                60,
                IsRecurring: true),
            capacity: capacity,
            waitlistEnabled: waitlistEnabled);
        gymsDb.GymClasses.Add(gymClass);
        await gymsDb.SaveChangesAsync();
        return gymClass;
    }

    private async Task BookMember(Guid classId, Guid memberId)
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gymClass = await gymsDb.GymClasses.Include(c => c.Bookings)
            .FirstAsync(c => c.Id == classId);
        var booking = gymClass.Book(memberId).Value;
        gymsDb.GymClasses.Update(gymClass);
        await gymsDb.SaveChangesAsync();
        _ = booking;
    }

    private async Task MarkAttended(Guid classId, Guid memberId)
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gymClass = await gymsDb.GymClasses.Include(c => c.Bookings)
            .FirstAsync(c => c.Id == classId);
        var booking = gymClass.Bookings.First(b => b.MemberId == memberId);
        booking.MarkAttended();
        gymsDb.GymClasses.Update(gymClass);
        await gymsDb.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------
    // GET /classes/{classId}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetClassById_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await HttpClient.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClassById_NonExistent_ReturnsNotFound()
    {
        var client = CreateTraineeClient();

        var response = await client.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClassById_Existing_ReturnsOkWithDto()
    {
        var gym = await SeedActiveGym();
        var gymClass = await SeedActiveClass(gym.Id);
        var client = CreateTraineeClient(gymId: gym.Id);

        var response = await client.GetAsync(
            $"api/v1/gyms/{gym.Id}/classes/{gymClass.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<GymClassDto>>(JsonOptions);
        payload!.Data!.Id.Should().Be(gymClass.Id);
        payload.Data.GymId.Should().Be(gym.Id);
        payload.Data.Name.Should().Be("Morning Yoga");
    }

    [Fact]
    public async Task GetClassById_WrongGym_ReturnsNotFound()
    {
        var gym = await SeedActiveGym();
        var gymClass = await SeedActiveClass(gym.Id);
        var client = CreateTraineeClient();

        var response = await client.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{gymClass.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // DELETE /classes/{classId}  (soft-delete via Cancel)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteClass_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await HttpClient.DeleteAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteClass_NonExistent_ReturnsNotFound()
    {
        var client = CreateGymOwnerClient();

        var response = await client.DeleteAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteClass_Existing_ReturnsNoContent_AndRemovesFromList()
    {
        var gym = await SeedActiveGym();
        var gymClass = await SeedActiveClass(gym.Id);
        var client = CreateGymOwnerClient(gymId: gym.Id);

        var deleteResponse = await client.DeleteAsync(
            $"api/v1/gyms/{gym.Id}/classes/{gymClass.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Soft-delete: row still exists in Cancelled state
        var db = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var refetched = await db.GymClasses.AsNoTracking()
            .FirstAsync(c => c.Id == gymClass.Id);
        refetched.Status.Should().Be(GymClassStatus.Cancelled);

        // List should no longer include the deleted class
        var listResponse = await client.GetAsync($"api/v1/gyms/{gym.Id}/classes");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<GymClassDto>>>(JsonOptions);
        list!.Data!.Should().NotContain(c => c.Id == gymClass.Id);

        // Detail should now 404
        var detailResponse = await client.GetAsync(
            $"api/v1/gyms/{gym.Id}/classes/{gymClass.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // GET /classes/{classId}/waitlist
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetWaitlist_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await HttpClient.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}/waitlist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWaitlist_NonExistentClass_ReturnsNotFound()
    {
        var client = CreateTraineeClient();

        var response = await client.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}/waitlist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWaitlist_ReturnsOnlyWaitlistedMembers()
    {
        var gym = await SeedActiveGym();
        // capacity 1 + waitlist on so the second booking is waitlisted
        var gymClass = await SeedActiveClass(gym.Id, capacity: 1, waitlistEnabled: true);
        var confirmed = Guid.NewGuid();
        var waitlisted = Guid.NewGuid();
        await BookMember(gymClass.Id, confirmed);
        await BookMember(gymClass.Id, waitlisted);

        var client = CreateGymOwnerClient(gymId: gym.Id);
        var response = await client.GetAsync(
            $"api/v1/gyms/{gym.Id}/classes/{gymClass.Id}/waitlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<MemberDto>>>(JsonOptions);
        payload!.Data!.Should().ContainSingle();
        payload.Data![0].MemberId.Should().Be(waitlisted);
    }

    // -----------------------------------------------------------------------
    // GET /classes/{classId}/attendance
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAttendance_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await HttpClient.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}/attendance");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAttendance_NonExistentClass_ReturnsNotFound()
    {
        var client = CreateTraineeClient();

        var response = await client.GetAsync(
            $"api/v1/gyms/{Guid.NewGuid()}/classes/{Guid.NewGuid()}/attendance");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAttendance_ReturnsOnlyAttendedMembers()
    {
        var gym = await SeedActiveGym();
        var gymClass = await SeedActiveClass(gym.Id, capacity: 5);
        var attended = Guid.NewGuid();
        var bookedOnly = Guid.NewGuid();
        await BookMember(gymClass.Id, attended);
        await BookMember(gymClass.Id, bookedOnly);
        await MarkAttended(gymClass.Id, attended);

        var client = CreateGymOwnerClient(gymId: gym.Id);
        var response = await client.GetAsync(
            $"api/v1/gyms/{gym.Id}/classes/{gymClass.Id}/attendance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<MemberDto>>>(JsonOptions);
        payload!.Data!.Should().ContainSingle();
        payload.Data![0].MemberId.Should().Be(attended);
    }
}

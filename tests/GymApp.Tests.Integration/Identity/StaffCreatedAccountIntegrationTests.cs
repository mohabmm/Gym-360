using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Identity;

/// <summary>
/// Accounts the desk creates: POST staff-create-trainer ("Add coach") and staff-create
/// ("Add member"). New accounts get the fixed password 12345678; an existing coach keeps
/// their own.
/// </summary>
public sealed class StaffCreatedAccountIntegrationTests : BaseIntegrationTest
{
    private const string Url = "api/v1/identity/users/staff-create-trainer";
    private const string DefaultPassword = "12345678";

    public StaffCreatedAccountIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    private sealed record CreatedTrainer(Guid UserId, string? InitialPassword);

    private static string UniquePhone() => $"+2010{Random.Shared.Next(10_000_000, 99_999_999)}";

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        HttpClient.PostAsJsonAsync("api/v1/identity/login", new { Email = email, Password = password });

    private static async Task<CreatedTrainer> ReadCreatedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreatedTrainer>>();
        return body!.Data!;
    }

    [Fact]
    public async Task NewTrainer_GetsDefaultPassword_AndCanLogInWithIt()
    {
        var email = $"coach-{Guid.NewGuid():N}@test.com";
        var owner = CreateGymOwnerClient(gymId: Guid.NewGuid());

        var created = await ReadCreatedAsync(await owner.PostAsJsonAsync(Url, new
        {
            FirstName = "Omar", LastName = "Coach", Phone = UniquePhone(), Email = email
        }));

        created.InitialPassword.Should().Be(DefaultPassword);
        (await LoginAsync(email, DefaultPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NewMember_GetsDefaultPassword_AndCanLogInWithIt()
    {
        var gymId = await SeedGymAsync();
        var email = $"member-{Guid.NewGuid():N}@test.com";
        var owner = CreateGymOwnerClient(gymId: gymId);

        var response = await owner.PostAsJsonAsync("api/v1/identity/users/staff-create", new
        {
            FirstName = "Sara", LastName = "Member", Phone = UniquePhone(), Email = email
        });

        var created = await ReadCreatedAsync(response);
        created.InitialPassword.Should().Be(DefaultPassword);
        (await LoginAsync(email, DefaultPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> SeedGymAsync()
    {
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = $"Password Test Gym {Guid.NewGuid():N}",
                Description = "Fixture gym for staff-created accounts",
                Address = new Address("1 Test St", "Cairo", "Cairo", "EG", "11511", 30.04, 31.24),
                ContactInfo = new ContactInfo("pw@test.local", "+201000000000", null),
                GymCode = GymCode.Generate()
            });
        gym.Approve(); // members can only join an active gym
        await GetService<IGymRepository>().AddAsync(gym);
        await GetService<IGymsUnitOfWork>().SaveChangesAsync();
        return gym.Id;
    }

    [Fact]
    public async Task ExistingTrainer_AddedByAnotherGym_KeepsTheirOwnPassword()
    {
        // Security regression: this used to reset the coach's password to the default,
        // letting any gym log in as another gym's coach just by entering their phone.
        var email = $"coach-{Guid.NewGuid():N}@test.com";
        var phone = UniquePhone();
        const string ownPassword = "MyOwnP@ss123";
        var register = await HttpClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email, Password = ownPassword, FirstName = "Omar", LastName = "Coach",
            Phone = phone, Role = "Trainer"
        });
        register.IsSuccessStatusCode.Should().BeTrue(await register.Content.ReadAsStringAsync());

        var otherGymOwner = CreateGymOwnerClient(gymId: Guid.NewGuid());
        var created = await ReadCreatedAsync(await otherGymOwner.PostAsJsonAsync(Url, new
        {
            FirstName = "Omar", LastName = "Coach", Phone = phone, Email = (string?)null
        }));

        created.InitialPassword.Should().BeNull();
        (await LoginAsync(email, DefaultPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoginAsync(email, ownPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

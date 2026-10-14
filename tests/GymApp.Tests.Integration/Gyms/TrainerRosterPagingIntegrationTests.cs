using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// Guards the trainer-scoped roster (GET /gyms/{id}/members?trainerId=…): the client
/// restriction must be applied BEFORE paging. It used to be applied to the fetched page,
/// so a trainer's clients on later pages disappeared and TotalCount was capped at the
/// page size (trainer Home reported "1 client" for a trainer with two).
/// </summary>
public sealed class TrainerRosterPagingIntegrationTests : BaseIntegrationTest
{
    public TrainerRosterPagingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private async Task<(Gym Gym, List<Guid> MemberIds)> SeedGymWithMembers(int count)
    {
        var gymsDb = Scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            ownerId: Guid.NewGuid(),
            name: $"Roster Test Gym {Guid.NewGuid():N}",
            description: null,
            address: new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
            contactInfo: new ContactInfo("gym@test.com", "+201234567890"),
            gymCode: GymCode.Create($"RST{Guid.NewGuid().ToString("N")[..3]}"));
        gym.Approve();

        var memberIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
        foreach (var id in memberIds)
            gym.AddMember(id).IsSuccess.Should().BeTrue();

        gymsDb.Gyms.Add(gym);
        await gymsDb.SaveChangesAsync();
        return (gym, memberIds);
    }

    [Fact]
    public async Task GetMembers_RestrictedToTrainerClients_PagesAndCountsOverTheRestrictedSet()
    {
        var (gym, memberIds) = await SeedGymWithMembers(5);
        // The trainer's clients sit at roster positions 1 and 3 — neither is on the first
        // page-of-one of the unrestricted roster.
        var clients = new[] { memberIds[1], memberIds[3] };
        var queries = GetService<IGymQueryService>();

        var page1 = await queries.GetMembersAsync(
            gym.Id, page: 1, pageSize: 1, status: null, section: null, search: null,
            restrictToMemberIds: clients);
        var page2 = await queries.GetMembersAsync(
            gym.Id, page: 2, pageSize: 1, status: null, section: null, search: null,
            restrictToMemberIds: clients);

        page1.TotalCount.Should().Be(2);
        page1.TotalPages.Should().Be(2);
        page1.Items.Select(m => m.MemberId).Should().Equal(memberIds[1]);
        page2.TotalCount.Should().Be(2);
        page2.Items.Select(m => m.MemberId).Should().Equal(memberIds[3]);
    }

    [Fact]
    public async Task GetMembers_TrainerWithNoClients_ReturnsEmptyPage()
    {
        var (gym, _) = await SeedGymWithMembers(3);

        var page = await GetService<IGymQueryService>().GetMembersAsync(
            gym.Id, page: 1, pageSize: 20, status: null, section: null, search: null,
            restrictToMemberIds: Array.Empty<Guid>());

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMembers_Unrestricted_StillReturnsWholeRoster()
    {
        var (gym, memberIds) = await SeedGymWithMembers(3);

        var page = await GetService<IGymQueryService>().GetMembersAsync(
            gym.Id, page: 1, pageSize: 20, status: null, section: null, search: null);

        page.TotalCount.Should().Be(3);
        page.Items.Select(m => m.MemberId).Should().Equal(memberIds);
    }
}

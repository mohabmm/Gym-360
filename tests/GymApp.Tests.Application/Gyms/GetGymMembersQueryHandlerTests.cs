using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetGymMembers;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Tests.Application.Gyms;

public class GetGymMembersQueryHandlerTests
{
    private readonly IGymQueryService _gymQueryService = Substitute.For<IGymQueryService>();
    private readonly ITrainerAssignmentLookup _trainerAssignmentLookup = Substitute.For<ITrainerAssignmentLookup>();
    private readonly GetGymMembersQueryHandler _sut;

    private readonly Guid _gymId = Guid.NewGuid();

    public GetGymMembersQueryHandlerTests()
    {
        _sut = new GetGymMembersQueryHandler(_gymQueryService, _trainerAssignmentLookup);
        _gymQueryService
            .GetMembersAsync(default, default!, default)
            .ReturnsForAnyArgs(new PagedResponse<MemberDto>(new List<MemberDto>(), 1, 1, 0, 0));
    }

    // Regression: the trainer filter used to be applied to the already-fetched page, so
    // TotalCount could never exceed the page size and clients on later pages vanished
    // (trainer Home showed "1 client" while the roster listed two).
    [Fact]
    public async Task Handle_WithTrainerId_RestrictsRosterBeforePaging()
    {
        var trainerId = Guid.NewGuid();
        var clients = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _trainerAssignmentLookup
            .GetClientsForTrainerAsync(trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(clients);
        var paged = new PagedResponse<MemberDto>(
            new List<MemberDto> { new(clients[0], _gymId, null, "0001", DateTime.UtcNow) },
            Page: 1, PageSize: 1, TotalCount: 2, TotalPages: 2);
        _gymQueryService
            .GetMembersAsync(
                new GymId(_gymId),
                Arg.Is<MemberRosterRequest>(r =>
                    r.Page == 1 && r.PageSize == 1 && r.RestrictToMemberIds != null && r.RestrictToMemberIds.SequenceEqual(clients)),
                Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _sut.Handle(
            new GetGymMembersQuery(_gymId, 1, 1, null, null, trainerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(paged, "paging and totals must come from the restricted query");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithoutTrainerId_DoesNotRestrictRoster()
    {
        await _sut.Handle(new GetGymMembersQuery(_gymId, 1, 20, null, null), CancellationToken.None);

        await _gymQueryService.Received(1).GetMembersAsync(
            new GymId(_gymId), new MemberRosterRequest(1, 20), Arg.Any<CancellationToken>());
        await _trainerAssignmentLookup.DidNotReceiveWithAnyArgs()
            .GetClientsForTrainerAsync(default, default, default);
    }
}

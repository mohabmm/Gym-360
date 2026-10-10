using GymApp.Modules.Billing.Application.Queries.SessionPackages;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Application.Billing;

/// <summary>
/// GET /api/v1/billing/packages — the staff "Active Packages" list calls it with
/// only the gym, and must get every package in that gym back.
/// </summary>
public class GetSessionPackagesQueryHandlerTests
{
    private readonly ISessionPackageRepository _packageRepository = Substitute.For<ISessionPackageRepository>();
    private readonly GetSessionPackagesQueryHandler _handler;
    private readonly Guid _gymId = Guid.NewGuid();

    public GetSessionPackagesQueryHandlerTests()
    {
        _handler = new GetSessionPackagesQueryHandler(_packageRepository);
    }

    private SessionPackage Package(Guid? trainerId = null, Guid? memberId = null) =>
        SessionPackage.Create(
            _gymId,
            trainerId ?? Guid.NewGuid(),
            memberId ?? Guid.NewGuid(),
            templateId: Guid.NewGuid(),
            name: "10 Sessions",
            totalSessions: 10,
            pricePerSession: Money.Create(500m),
            totalPrice: Money.Create(5000m),
            assignedBy: Guid.NewGuid());

    [Fact]
    public async Task Handle_WithoutMemberOrTrainer_ReturnsEveryPackageInTheGym()
    {
        var packages = new List<SessionPackage> { Package(), Package() };
        _packageRepository.GetByGymAsync(_gymId, Arg.Any<CancellationToken>()).Returns(packages);

        var result = await _handler.Handle(
            new GetSessionPackagesQuery(_gymId, TrainerId: null, MemberId: null, Status: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        await _packageRepository.DidNotReceive()
            .GetByTrainerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMember_QueriesThatMemberOnly()
    {
        var memberId = Guid.NewGuid();
        _packageRepository.GetByMemberAsync(_gymId, memberId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage> { Package(memberId: memberId) });

        var result = await _handler.Handle(
            new GetSessionPackagesQuery(_gymId, TrainerId: null, MemberId: memberId, Status: null),
            CancellationToken.None);

        result.Value.Should().ContainSingle().Which.MemberId.Should().Be(memberId);
        await _packageRepository.DidNotReceive().GetByGymAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTrainer_QueriesThatTrainerOnly()
    {
        var trainerId = Guid.NewGuid();
        _packageRepository.GetByTrainerAsync(_gymId, trainerId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage> { Package(trainerId: trainerId) });

        var result = await _handler.Handle(
            new GetSessionPackagesQuery(_gymId, TrainerId: trainerId, MemberId: null, Status: null),
            CancellationToken.None);

        result.Value.Should().ContainSingle().Which.TrainerId.Should().Be(trainerId);
    }

    [Fact]
    public async Task Handle_GymWideWithStatus_FiltersByStatus()
    {
        _packageRepository.GetByGymAsync(_gymId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage> { Package(), Package() });

        var active = await _handler.Handle(
            new GetSessionPackagesQuery(_gymId, null, null, "active"), CancellationToken.None);
        var exhausted = await _handler.Handle(
            new GetSessionPackagesQuery(_gymId, null, null, "Exhausted"), CancellationToken.None);

        active.Value.Should().HaveCount(2);
        exhausted.Value.Should().BeEmpty();
    }
}

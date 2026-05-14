using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Commands.Deploy;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Tests.Application.Billing;

public class DeployChangesCommandHandlerTests
{
    private readonly IPlanChangeLogRepository _changeLogRepository = Substitute.For<IPlanChangeLogRepository>();
    private readonly IBillingUnitOfWork _unitOfWork = Substitute.For<IBillingUnitOfWork>();
    private readonly DeployChangesCommandHandler _sut;

    public DeployChangesCommandHandlerTests()
    {
        _sut = new DeployChangesCommandHandler(_changeLogRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_GymScope_DeploysForSingleGym()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var command = new DeployChangesCommand("gym", targetId, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scope.Should().Be("gym");
        result.Value.TargetId.Should().Be(targetId);
        result.Value.AffectedGyms.Should().Be(1);
        await _changeLogRepository.Received(1).AddAsync(Arg.Any<PlanChangeLog>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllScope_DeploysForAll()
    {
        // Arrange
        var command = new DeployChangesCommand("all", null, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scope.Should().Be("all");
        result.Value.AffectedGyms.Should().Be(-1); // Resolved later
    }

    [Fact]
    public async Task Handle_PlanScope_DeploysForPlan()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command = new DeployChangesCommand("plan", planId, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scope.Should().Be("plan");
    }
}

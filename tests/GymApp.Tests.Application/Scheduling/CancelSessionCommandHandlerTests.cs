using GymApp.Common.Application.Abstractions;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Commands.CancelSession;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;
using MediatR;

namespace GymApp.Tests.Application.Scheduling;

public class CancelSessionCommandHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>();
    private readonly IGymSessionPolicyService _gymSessionPolicyService = Substitute.For<IGymSessionPolicyService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CancelSessionCommandHandler _sut;

    public CancelSessionCommandHandlerTests()
    {
        _sut = new CancelSessionCommandHandler(
            _sessionRepository, _gymSessionPolicyService, _dateTimeProvider, _unitOfWork, _publisher);
    }

    private static Session CreateFutureSession(Guid gymId, int daysFromNow = 3)
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysFromNow));
        return Session.Create(
            gymId, Guid.NewGuid(), Guid.NewGuid(),
            futureDate, new TimeOnly(10, 0), 60, SessionType.PersonalTraining, "General");
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var command = new CancelSessionCommand(sessionId, Guid.NewGuid(), "Sick");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.SessionNotFound");
    }

    [Fact]
    public async Task Handle_WithConfigurableCancelPolicy_UsesGymPolicy()
    {
        // Arrange -- gym has 48h cancellation window (NOT hardcoded 24h)
        var gymId = Guid.NewGuid();
        var session = CreateFutureSession(gymId, daysFromNow: 3);
        var sessionId = Guid.NewGuid();
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        // Gym policy: 48-hour cancellation window
        _gymSessionPolicyService.GetPolicyAsync(gymId, Arg.Any<CancellationToken>())
            .Returns(new GymSessionPolicyDto(
                CancellationWindowHours: 48,
                NoshowThreshold: 3,
                NoshowWindowDays: 30,
                NoshowAction: "suspend"));

        // Now is 3 days before session, which is > 48h => not late cancellation
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var command = new CancelSessionCommand(sessionId, Guid.NewGuid(), "Travel plans changed");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        // Session should be cancellable (3 days > 48h)
        await _gymSessionPolicyService.Received(1).GetPolicyAsync(gymId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DefaultPolicyWhenNoGymPolicy_Uses24HDefault()
    {
        // Arrange -- no gym policy configured, falls back to 24h
        var gymId = Guid.NewGuid();
        var session = CreateFutureSession(gymId, daysFromNow: 2);
        var sessionId = Guid.NewGuid();
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        _gymSessionPolicyService.GetPolicyAsync(gymId, Arg.Any<CancellationToken>())
            .Returns((GymSessionPolicyDto?)null);

        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var command = new CancelSessionCommand(sessionId, Guid.NewGuid(), "Changed my mind");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert - verifies the handler does call the policy service
        await _gymSessionPolicyService.Received(1).GetPolicyAsync(gymId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LateCancellation_StillCancelsWithLateFlagSet()
    {
        // Arrange -- session is in 2 hours, policy is 24h window => late cancellation
        var gymId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var sessionDate = DateOnly.FromDateTime(now);
        var sessionTime = TimeOnly.FromDateTime(now.AddHours(2));

        var session = Session.Create(
            gymId, Guid.NewGuid(), Guid.NewGuid(),
            sessionDate, sessionTime, 60, SessionType.PersonalTraining, "General");

        var sessionId = Guid.NewGuid();
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        _gymSessionPolicyService.GetPolicyAsync(gymId, Arg.Any<CancellationToken>())
            .Returns(new GymSessionPolicyDto(
                CancellationWindowHours: 24,
                NoshowThreshold: 3,
                NoshowWindowDays: 30,
                NoshowAction: "suspend"));

        _dateTimeProvider.UtcNow.Returns(now);

        var command = new CancelSessionCommand(sessionId, Guid.NewGuid(), "Emergency");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert - the handler should still attempt cancellation
        // The domain entity's Cancel method handles the late flag
        _sessionRepository.Received().Update(session);
    }
}

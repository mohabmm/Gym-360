using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.IntegrationEvents;

namespace GymApp.Tests.Application.Pt;

public class QuickLogSessionTests
{
    private readonly ISessionRepository _sessions = Substitute.For<ISessionRepository>();
    private readonly ISessionPackageService _packages = Substitute.For<ISessionPackageService>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IIntegrationEventPublisher _events = Substitute.For<IIntegrationEventPublisher>();
    private readonly ISchedulingUnitOfWork _uow = Substitute.For<ISchedulingUnitOfWork>();
    private readonly QuickLogSessionCommandHandler _sut;

    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Client = Guid.NewGuid();
    private readonly QuickLogSessionCommand _command = new(Gym, Coach, Client, null);

    public QuickLogSessionTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 11, 12, 16, 0, 0, DateTimeKind.Utc));
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtDualSessionLogging, Arg.Any<CancellationToken>()).Returns(true);
        _sut = new QuickLogSessionCommandHandler(new QuickLogStateLoader(_sessions, _packages, _clock), _sessions, _flags, _events, _uow);
    }

    [Fact]
    public async Task One_tap_logs_a_completed_session_and_publishes_completion()
    {
        _packages.GetRemainingWithTrainerAsync(Client, Coach, Gym, Arg.Any<CancellationToken>()).Returns(5);

        var result = await _sut.Handle(_command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RemainingSessions.Should().Be(4);
        result.Value.AlreadyLogged.Should().BeFalse();
        await _sessions.Received(1).AddAsync(Arg.Is<Session>(s => s.IsQuickLog), Arg.Any<CancellationToken>());
        _events.Received(1).Enqueue(Arg.Is<SessionCompletedIntegrationEvent>(e => e.MemberId == Client && e.TrainerId == Coach));
    }

    [Fact]
    public async Task Repeat_tap_within_five_minutes_returns_the_same_session()
    {
        var existing = Session.LogCompleted(
            new SessionParticipants(Gym, Coach, Client), new SessionSchedule(new DateOnly(2026, 11, 12), new TimeOnly(18, 0), 60), null);
        _packages.GetRemainingWithTrainerAsync(Client, Coach, Gym, Arg.Any<CancellationToken>()).Returns(4);
        _sessions.FindRecentQuickLogAsync(Coach, Client, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.Handle(_command, CancellationToken.None);

        result.Value.SessionId.Should().Be(existing.Id);
        result.Value.AlreadyLogged.Should().BeTrue();
        await _sessions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Client_without_a_package_with_this_coach_is_rejected()
    {
        _packages.GetRemainingWithTrainerAsync(Client, Coach, Gym, Arg.Any<CancellationToken>()).Returns((int?)null);

        var result = await _sut.Handle(_command, CancellationToken.None);

        result.Error.Should().Be(SchedulingErrors.ClientNotAssignedToTrainer);
    }

    [Fact]
    public async Task Package_with_no_sessions_left_is_rejected()
    {
        _packages.GetRemainingWithTrainerAsync(Client, Coach, Gym, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.Handle(_command, CancellationToken.None);

        result.Error.Should().Be(SchedulingErrors.NoSessionsRemaining);
    }

    [Fact]
    public async Task Blocked_when_dual_session_logging_is_off()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtDualSessionLogging, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(_command, CancellationToken.None);

        result.Error.Code.Should().Be("FEATURE_DISABLED:pt.dual_session_logging");
    }
}

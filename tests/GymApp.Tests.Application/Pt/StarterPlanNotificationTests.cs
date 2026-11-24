using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Application.IntegrationEventHandlers;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Training.IntegrationEvents;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymApp.Tests.Application.Pt;

/// <summary>pt.starter_plan: the coach hears about a new client and about unlocked programs; the tap opens the member.</summary>
public class StarterPlanNotificationTests
{
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();

    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();
    private readonly StarterPlanIntegrationEventHandlers _sut;
    private readonly List<(Guid To, PushContent Content)> _pushed = new();

    public StarterPlanNotificationTests()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtStarterPlan, Arg.Any<CancellationToken>()).Returns(true);
        var users = Substitute.For<IUserDisplayLookup>();
        users.GetDisplayNamesAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, UserDisplay>());
        _push.WhenForAnyArgs(p => p.SendAsync(default, default!, default))
            .Do(ci => _pushed.Add((ci.ArgAt<Guid>(0), ci.ArgAt<PushContent>(1))));

        var notifier = new PtNotifier(
            Substitute.For<INotificationRepository>(),
            Substitute.For<INotificationPreferenceRepository>(),
            new PtPushSender(_push, NullLogger<PtPushSender>.Instance),
            users,
            Substitute.For<ICommunicationUnitOfWork>());
        _sut = new StarterPlanIntegrationEventHandlers(_flags, notifier);
    }

    private static SessionPackageAssignedIntegrationEvent Assigned(bool trial) =>
        new(Guid.NewGuid(), Gym, Member, Coach, trial, "pkg", trial ? 2 : 12);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Every_package_tells_only_the_coach_and_opens_the_member(bool trial)
    {
        await _sut.Handle(Assigned(trial), CancellationToken.None);

        _pushed.Should().ContainSingle();
        _pushed[0].To.Should().Be(Coach);
        _pushed[0].Content.Data!["type"].Should().Be(StarterPlanIntegrationEventHandlers.NewClientType);
        _pushed[0].Content.Data!["route"].Should().Be($"/clients/{Member}");
    }

    [Fact]
    public async Task Nothing_is_sent_when_the_flag_is_off()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtStarterPlan, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.Handle(Assigned(trial: true), CancellationToken.None);
        await _sut.Handle(new PlanProgramsUnlockedIntegrationEvent(Gym, Coach, Member, Guid.NewGuid()), CancellationToken.None);

        _pushed.Should().BeEmpty();
    }

    [Fact]
    public async Task Unlocking_tells_the_coach()
    {
        await _sut.Handle(new PlanProgramsUnlockedIntegrationEvent(Gym, Coach, Member, Guid.NewGuid()), CancellationToken.None);

        _pushed.Should().ContainSingle(p => p.To == Coach
            && p.Content.Data!["type"] == StarterPlanIntegrationEventHandlers.ProgramsUnlockedType);
    }

    [Fact]
    public async Task A_30_day_unlock_says_30_days()
    {
        await _sut.Handle(new PlanProgramsUnlockedIntegrationEvent(Gym, Coach, Member, Guid.NewGuid(), LengthDays: 30), CancellationToken.None);

        _pushed.Should().ContainSingle(p => p.Content.Title.Contains("٣٠ يوم"));
    }
}

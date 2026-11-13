using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Application.IntegrationEventHandlers;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Identity.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Milestones = GymApp.Modules.Billing.IntegrationEvents.SessionPackageMilestoneReachedIntegrationEvent.Milestones;

namespace GymApp.Tests.Application.Pt;

/// <summary>Who hears about 3/1 renewal reminders and trial alerts, and that flags switch them off.</summary>
public class PtReminderRecipientsTests
{
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Desk = Guid.NewGuid();

    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();
    private readonly SessionPackageMilestoneReachedIntegrationEventHandler _sut;

    public PtReminderRecipientsTests()
    {
        _flags.IsEnabledAsync(Gym, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var desk = Substitute.For<IGymDeskLookup>();
        desk.GetDeskUserIdsAsync(Gym, Arg.Any<CancellationToken>()).Returns(new[] { Desk });
        var users = Substitute.For<IUserDisplayLookup>();
        users.GetDisplayNamesAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, UserDisplay>());

        var notifier = new PtNotifier(
            Substitute.For<INotificationRepository>(),
            Substitute.For<INotificationPreferenceRepository>(),
            new PtPushSender(_push, NullLogger<PtPushSender>.Instance),
            users,
            Substitute.For<ICommunicationUnitOfWork>());

        _sut = new SessionPackageMilestoneReachedIntegrationEventHandler(_flags, desk, notifier);
    }

    private static SessionPackageMilestoneReachedIntegrationEvent Event(string milestone, int remaining, bool trial = false) =>
        new(Guid.NewGuid(), Gym, Member, Coach, milestone, remaining, 8, trial);

    private async Task<HashSet<Guid>> PushedToAsync(SessionPackageMilestoneReachedIntegrationEvent e)
    {
        var pushed = new HashSet<Guid>();
        await _push.SendAsync(Arg.Do<Guid>(id => pushed.Add(id)), Arg.Any<PushContent>(), Arg.Any<CancellationToken>());
        await _sut.Handle(e, CancellationToken.None);
        return pushed;
    }

    [Fact]
    public async Task Three_left_reaches_trainee_and_coach()
    {
        (await PushedToAsync(Event(Milestones.RenewalThreeLeft, 3))).Should().BeEquivalentTo(new[] { Member, Coach });
    }

    [Fact]
    public async Task One_left_also_reaches_the_desk()
    {
        (await PushedToAsync(Event(Milestones.RenewalOneLeft, 1))).Should().BeEquivalentTo(new[] { Member, Coach, Desk });
    }

    [Fact]
    public async Task Last_free_session_reaches_coach_and_desk()
    {
        (await PushedToAsync(Event(Milestones.TrialLastSession, 1, trial: true))).Should().BeEquivalentTo(new[] { Coach, Desk });
    }

    [Fact]
    public async Task Trial_finished_reaches_coach_desk_and_trainee()
    {
        (await PushedToAsync(Event(Milestones.TrialFinished, 0, trial: true))).Should().BeEquivalentTo(new[] { Coach, Desk, Member });
    }

    [Fact]
    public async Task Trial_finished_skips_the_trainee_when_follow_ups_are_off()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtTrialFollowUps, Arg.Any<CancellationToken>()).Returns(false);

        (await PushedToAsync(Event(Milestones.TrialFinished, 0, trial: true))).Should().BeEquivalentTo(new[] { Coach, Desk });
    }

    [Theory]
    [InlineData(Milestones.TrialFollowUpDay3)]
    [InlineData(Milestones.TrialLastChanceDay12)]
    public async Task Follow_up_reminders_reach_only_the_trainee(string milestone)
    {
        (await PushedToAsync(Event(milestone, 0, trial: true))).Should().BeEquivalentTo(new[] { Member });
    }

    [Theory]
    [InlineData(Milestones.TrialFollowUpDay3)]
    [InlineData(Milestones.TrialLastChanceDay12)]
    public async Task Follow_up_reminders_flag_off_sends_nothing(string milestone)
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtTrialFollowUps, Arg.Any<CancellationToken>()).Returns(false);

        (await PushedToAsync(Event(milestone, 0, trial: true))).Should().BeEmpty();
    }

    [Fact]
    public async Task Renewal_reminders_flag_off_sends_nothing()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtRenewalReminders, Arg.Any<CancellationToken>()).Returns(false);

        (await PushedToAsync(Event(Milestones.RenewalOneLeft, 1))).Should().BeEmpty();
    }

    [Fact]
    public async Task Trial_alerts_and_follow_ups_off_sends_nothing()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtTrialEndAlerts, Arg.Any<CancellationToken>()).Returns(false);
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtTrialFollowUps, Arg.Any<CancellationToken>()).Returns(false);

        (await PushedToAsync(Event(Milestones.TrialFinished, 0, trial: true))).Should().BeEmpty();
    }
}

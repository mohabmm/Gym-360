using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.Commands.PackageInterests;
using GymApp.Modules.Billing.Application.Commands.TrialFollowUps;
using GymApp.Modules.Billing.Application.Commands.TrialRecaps;
using GymApp.Modules.Billing.Application.Queries.PackageInterests;
using GymApp.Modules.Billing.Application.Services;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Tests.Application.Pt;

/// <summary>Trial-to-paid handlers: flags gate them, recap/lead rules hold, follow-ups skip trainees who moved on.</summary>
public class TrialConversionHandlerTests
{
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 13, 7, 0, 0, DateTimeKind.Utc);

    private readonly ISessionPackageRepository _packages = Substitute.For<ISessionPackageRepository>();
    private readonly IPackageInterestRepository _interests = Substitute.For<IPackageInterestRepository>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IIntegrationEventPublisher _events = Substitute.For<IIntegrationEventPublisher>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IBillingUnitOfWork _uow = Substitute.For<IBillingUnitOfWork>();

    public TrialConversionHandlerTests()
    {
        _flags.IsEnabledAsync(Gym, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _clock.UtcNow.Returns(Now);
        _packages.GetByMemberAsync(default, default, default).ReturnsForAnyArgs(Array.Empty<SessionPackage>());
        _interests.GetByMemberAsync(default!, default).ReturnsForAnyArgs(Array.Empty<PackageInterest>());
    }

    private static SessionPackage Package(int sessions, bool isTrial, Guid? member = null) =>
        SessionPackage.Create(new SessionPackageDetails
        {
            GymId = Gym,
            TrainerId = Guid.NewGuid(),
            MemberId = member ?? Guid.NewGuid(),
            TemplateId = Guid.Empty,
            Name = "P",
            TotalSessions = sessions,
            PricePerSession = Money.Create(isTrial ? 0m : 100m, "EGP"),
            TotalPrice = Money.Create(isTrial ? 0m : sessions * 100m, "EGP"),
            AssignedBy = Guid.NewGuid(),
            IsTrial = isTrial,
        });

    private SessionPackage Stored(SessionPackage package)
    {
        _packages.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        return package;
    }

    // ---- pt.trial_recap ----

    private SendTrialRecapCommandHandler RecapHandler() => new(_packages, _flags, _events, _clock, _uow);

    [Fact]
    public async Task Coach_sends_recap_and_the_trainee_is_told()
    {
        var trial = Stored(Package(2, isTrial: true));

        var result = await RecapHandler().Handle(
            new SendTrialRecapCommand(Gym, trial.Id, trial.TrainerId, "Great start", 12), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        trial.RecommendedSessions.Should().Be(12);
        _events.Received(1).Enqueue(Arg.Is<TrialRecapSentIntegrationEvent>(e => e.MemberId == trial.MemberId && e.RecommendedSessions == 12));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(FeatureKeys.PtTrialRecap)]
    [InlineData(FeatureKeys.PtTrialSessions)]
    public async Task Recap_is_blocked_when_a_flag_is_off(string flag)
    {
        _flags.IsEnabledAsync(Gym, flag, Arg.Any<CancellationToken>()).Returns(false);
        var trial = Stored(Package(2, isTrial: true));

        var result = await RecapHandler().Handle(
            new SendTrialRecapCommand(Gym, trial.Id, trial.TrainerId, "Plan", 12), CancellationToken.None);

        result.Error.Code.Should().Be($"FEATURE_DISABLED:{flag}");
        _events.DidNotReceiveWithAnyArgs().Enqueue(default!);
    }

    [Fact]
    public async Task Recap_for_another_gyms_package_is_not_found()
    {
        var trial = Stored(Package(2, isTrial: true));
        var otherGym = Guid.NewGuid();
        _flags.IsEnabledAsync(otherGym, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await RecapHandler().Handle(
            new SendTrialRecapCommand(otherGym, trial.Id, trial.TrainerId, "Plan", 12), CancellationToken.None);

        result.Error.Should().Be(BillingErrors.PackageNotFound);
    }

    // ---- pt.package_interest ----

    private ExpressPackageInterestCommandHandler InterestHandler() => new(_packages, _interests, _flags, _events, _clock, _uow);

    [Fact]
    public async Task Trainee_interest_creates_a_lead_and_notifies()
    {
        var trial = Stored(Package(2, isTrial: true));
        PackageInterest? saved = null;
        await _interests.AddAsync(Arg.Do<PackageInterest>(i => saved = i), Arg.Any<CancellationToken>());

        var result = await InterestHandler().Handle(
            new ExpressPackageInterestCommand(Gym, trial.MemberId, trial.Id), CancellationToken.None);

        result.Value.AlreadyPending.Should().BeFalse();
        saved!.MemberId.Should().Be(trial.MemberId);
        _events.Received(1).Enqueue(Arg.Is<PackageInterestCreatedIntegrationEvent>(e => e.InterestId == saved.Id && e.Source == "TrialFollowUp"));
    }

    [Fact]
    public async Task Second_tap_returns_the_pending_lead()
    {
        var trial = Stored(Package(2, isTrial: true));
        var pending = PackageInterest.ForPackage(trial, Now.AddHours(-1));
        _interests.GetByMemberAsync(Arg.Any<PackageOwner>(), Arg.Any<CancellationToken>()).Returns(new[] { pending });

        var result = await InterestHandler().Handle(
            new ExpressPackageInterestCommand(Gym, trial.MemberId, trial.Id), CancellationToken.None);

        result.Value.Should().Be(new PackageInterestReceipt(pending.Id, AlreadyPending: true));
        await _interests.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        _events.DidNotReceiveWithAnyArgs().Enqueue(default!);
    }

    [Fact]
    public async Task A_new_lead_is_allowed_after_the_previous_one_converted()
    {
        var member = Guid.NewGuid();
        var paid = Stored(Package(8, isTrial: false, member));
        var oldLead = PackageInterest.ForPackage(Package(2, isTrial: true, member), paid.CreatedAt.AddDays(-1));
        _interests.GetByMemberAsync(Arg.Any<PackageOwner>(), Arg.Any<CancellationToken>()).Returns(new[] { oldLead });
        _packages.GetByMemberAsync(Gym, member, Arg.Any<CancellationToken>()).Returns(new[] { paid });

        var result = await InterestHandler().Handle(
            new ExpressPackageInterestCommand(Gym, member, paid.Id), CancellationToken.None);

        result.Value.AlreadyPending.Should().BeFalse();
    }

    [Fact]
    public async Task Interest_on_someone_elses_package_is_not_found()
    {
        var trial = Stored(Package(2, isTrial: true));

        var result = await InterestHandler().Handle(
            new ExpressPackageInterestCommand(Gym, Guid.NewGuid(), trial.Id), CancellationToken.None);

        result.Error.Should().Be(BillingErrors.PackageNotFound);
    }

    [Fact]
    public async Task Interest_is_blocked_when_the_flag_is_off()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtPackageInterest, Arg.Any<CancellationToken>()).Returns(false);
        var trial = Stored(Package(2, isTrial: true));

        var result = await InterestHandler().Handle(
            new ExpressPackageInterestCommand(Gym, trial.MemberId, trial.Id), CancellationToken.None);

        result.Error.Code.Should().Be("FEATURE_DISABLED:pt.package_interest");
    }

    [Fact]
    public async Task Desk_list_shows_pending_first_and_marks_converted_leads()
    {
        var waiting = PackageInterest.ForPackage(Package(2, isTrial: true), Now.AddDays(-2));
        var buyer = Guid.NewGuid();
        var converted = PackageInterest.ForPackage(Package(2, isTrial: true, buyer), Now.AddDays(-5));
        var bought = Package(8, isTrial: false, buyer);
        _interests.GetByGymAsync(Gym, Arg.Any<CancellationToken>()).Returns(new[] { waiting, converted });
        _packages.GetByGymAsync(Gym, Arg.Any<CancellationToken>()).Returns(new[] { bought });
        var profiles = Substitute.For<IUserProfileLookup>();
        profiles.GetProfilesAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, UserProfileInfo>());
        var sut = new GetPackageInterestsQueryHandler(_interests, _packages, profiles, _flags);

        var all = await sut.Handle(new GetPackageInterestsQuery(Gym, PackageInterestStates.All), CancellationToken.None);
        var pending = await sut.Handle(new GetPackageInterestsQuery(Gym, PackageInterestStates.Pending), CancellationToken.None);

        all.Value.Select(r => (r.Id, r.State)).Should().Equal(
            (waiting.Id, PackageInterestStates.Pending),
            (converted.Id, PackageInterestStates.Converted));
        pending.Value.Should().ContainSingle(r => r.Id == waiting.Id);
    }

    // ---- pt.trial_followups ----

    [Theory]
    [InlineData(2, null)]
    [InlineData(3, PackageMilestone.TrialFollowUpDay3)]
    [InlineData(11, PackageMilestone.TrialFollowUpDay3)]
    [InlineData(12, PackageMilestone.TrialLastChanceDay12)]
    [InlineData(13, PackageMilestone.TrialLastChanceDay12)]
    [InlineData(14, null)]
    public void Follow_up_schedule(int daysSinceTrial, PackageMilestone? expected)
    {
        TrialFollowUpSchedule.DueOn(Now.AddDays(-daysSinceTrial), Now).Should().Be(expected);
    }

    private SendTrialFollowUpsCommandHandler FollowUpHandler() => new(_packages, _interests, _flags, _events, _clock, _uow);

    private SessionPackage FinishedTrial(Guid? member = null)
    {
        var trial = Package(1, isTrial: true, member);
        trial.UseSession(Guid.NewGuid());
        trial.ClearDomainEvents();
        return trial;
    }

    [Fact]
    public async Task Day_three_reminder_is_raised_for_a_trainee_who_has_not_subscribed()
    {
        // ExhaustedAt is "now" (real clock), so move the handler clock 4 days ahead of it.
        var trial = FinishedTrial();
        _clock.UtcNow.Returns(trial.ExhaustedAt!.Value.AddDays(4));
        _packages.GetFinishedTrialsAsync(default, default).ReturnsForAnyArgs(new[] { trial });

        var result = await FollowUpHandler().Handle(new SendTrialFollowUpsCommand(), CancellationToken.None);

        result.Value.Should().Be(1);
        _events.Received(1).Enqueue(Arg.Is<SessionPackageMilestoneReachedIntegrationEvent>(
            e => e.Milestone == SessionPackageMilestoneReachedIntegrationEvent.Milestones.TrialFollowUpDay3 && e.MemberId == trial.MemberId));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_reminder_once_the_trainee_bought_a_package_or_tapped_interested()
    {
        var bought = FinishedTrial();
        var interested = FinishedTrial();
        _clock.UtcNow.Returns(bought.ExhaustedAt!.Value.AddDays(4));
        _packages.GetFinishedTrialsAsync(default, default).ReturnsForAnyArgs(new[] { bought, interested });
        _packages.GetByMemberAsync(Gym, bought.MemberId, Arg.Any<CancellationToken>())
            .Returns(new[] { Package(8, isTrial: false, bought.MemberId) });
        _interests.GetByMemberAsync(Arg.Is<PackageOwner>(o => o.MemberId == interested.MemberId), Arg.Any<CancellationToken>())
            .Returns(new[] { PackageInterest.ForPackage(interested, interested.CreatedAt.AddMinutes(1)) });

        var result = await FollowUpHandler().Handle(new SendTrialFollowUpsCommand(), CancellationToken.None);

        result.Value.Should().Be(0);
        _events.DidNotReceiveWithAnyArgs().Enqueue(default!);
    }

    [Fact]
    public async Task No_reminders_when_follow_ups_are_off()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtTrialFollowUps, Arg.Any<CancellationToken>()).Returns(false);
        var trial = FinishedTrial();
        _clock.UtcNow.Returns(trial.ExhaustedAt!.Value.AddDays(4));
        _packages.GetFinishedTrialsAsync(default, default).ReturnsForAnyArgs(new[] { trial });

        var result = await FollowUpHandler().Handle(new SendTrialFollowUpsCommand(), CancellationToken.None);

        result.Value.Should().Be(0);
        await _uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}

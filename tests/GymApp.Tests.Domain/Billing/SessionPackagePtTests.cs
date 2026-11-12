using FluentAssertions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Billing;

/// <summary>PT module rules on the package: remaining sessions, 3/1 renewal reminders, trial end detection, restore.</summary>
public sealed class SessionPackagePtTests
{
    private static SessionPackage Package(int sessions, bool isTrial = false) =>
        SessionPackage.Create(new SessionPackageDetails
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TemplateId = Guid.Empty,
            Name = isTrial ? "Trial" : "Paid",
            TotalSessions = sessions,
            PricePerSession = Money.Create(isTrial ? 0m : 100m, "EGP"),
            TotalPrice = Money.Create(isTrial ? 0m : sessions * 100m, "EGP"),
            AssignedBy = Guid.NewGuid(),
            IsTrial = isTrial,
        });

    private static List<PackageMilestone> UseAndCollectMilestones(SessionPackage package, int uses)
    {
        var milestones = new List<PackageMilestone>();
        for (var i = 0; i < uses; i++)
        {
            package.ClearDomainEvents();
            package.UseSession(Guid.NewGuid()).IsSuccess.Should().BeTrue();
            milestones.AddRange(package.GetDomainEvents().OfType<SessionPackageMilestoneReachedDomainEvent>().Select(e => e.Milestone));
        }

        return milestones;
    }

    [Fact]
    public void Remaining_sessions_count_down_and_package_is_exhausted_at_zero()
    {
        var package = Package(4);

        UseAndCollectMilestones(package, 4);

        package.RemainingSessions.Should().Be(0);
        package.UsedSessions.Should().Be(4);
        package.Status.Should().Be(PackageStatus.Exhausted);
        package.ExhaustedAt.Should().NotBeNull();
    }

    [Fact]
    public void Paid_package_reminds_exactly_at_three_and_at_one_left()
    {
        var package = Package(8);

        var milestones = UseAndCollectMilestones(package, 8);

        milestones.Should().Equal(PackageMilestone.RenewalThreeLeft, PackageMilestone.RenewalOneLeft);
    }

    [Fact]
    public void Each_reminder_fires_once_even_if_the_count_returns_to_it()
    {
        var package = Package(8);
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        ids.ForEach(id => package.UseSession(id));          // 3 left → reminder fired
        package.RestoreSession(ids[^1]).IsSuccess.Should().BeTrue(); // back to 4

        var again = UseAndCollectMilestones(package, 3);    // 3, 2, 1 left

        package.RemainingSessions.Should().Be(1);
        again.Should().Equal(PackageMilestone.RenewalOneLeft);
    }

    [Fact]
    public void Trial_alerts_at_last_free_session_and_when_finished_but_never_sends_renewal_reminders()
    {
        var trial = Package(3, isTrial: true);

        var milestones = UseAndCollectMilestones(trial, 3);

        milestones.Should().Equal(PackageMilestone.TrialLastSession, PackageMilestone.TrialFinished);
        trial.ExhaustedAt.Should().NotBeNull();
    }

    [Fact]
    public void One_session_trial_reports_finished_after_its_only_session()
    {
        var trial = Package(1, isTrial: true);

        UseAndCollectMilestones(trial, 1).Should().Equal(PackageMilestone.TrialFinished);
    }

    [Theory]
    [InlineData(false, 3, PackageMilestone.RenewalThreeLeft)]
    [InlineData(false, 1, PackageMilestone.RenewalOneLeft)]
    [InlineData(true, 1, PackageMilestone.TrialLastSession)]
    [InlineData(true, 0, PackageMilestone.TrialFinished)]
    public void MilestoneFor_maps_remaining_sessions(bool isTrial, int remaining, PackageMilestone expected)
    {
        SessionPackage.MilestoneFor(isTrial, remaining).Should().Be(expected);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 0)]
    [InlineData(false, 4)]
    [InlineData(true, 3)]
    [InlineData(true, 2)]
    public void MilestoneFor_is_silent_otherwise(bool isTrial, int remaining)
    {
        SessionPackage.MilestoneFor(isTrial, remaining).Should().BeNull();
    }

    [Fact]
    public void RestoreSession_gives_the_session_back_and_reactivates_an_exhausted_package()
    {
        var package = Package(4);
        UseAndCollectMilestones(package, 3);
        var last = Guid.NewGuid();
        package.UseSession(last);
        package.Status.Should().Be(PackageStatus.Exhausted);

        package.RestoreSession(last).IsSuccess.Should().BeTrue();

        package.RemainingSessions.Should().Be(1);
        package.Status.Should().Be(PackageStatus.Active);
        package.ExhaustedAt.Should().BeNull();
    }

    [Fact]
    public void RestoreSession_is_idempotent_and_ignores_unknown_sessions()
    {
        var package = Package(4);
        var sessionId = Guid.NewGuid();
        package.UseSession(sessionId);

        package.RestoreSession(sessionId);
        package.RestoreSession(sessionId);
        package.RestoreSession(Guid.NewGuid());

        package.RemainingSessions.Should().Be(4);
    }
}

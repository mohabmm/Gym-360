using FluentAssertions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Billing;

/// <summary>Trial-to-paid rules: coach recap (pt.trial_recap), leads (pt.package_interest), follow-ups (pt.trial_followups).</summary>
public sealed class TrialConversionDomainTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc);

    private static SessionPackage Package(int sessions, bool isTrial) =>
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

    private static SessionPackage FinishedTrial()
    {
        var trial = Package(1, isTrial: true);
        trial.UseSession(Guid.NewGuid()).IsSuccess.Should().BeTrue();
        trial.ClearDomainEvents();
        return trial;
    }

    private static TrialRecap Recap(int sessions = 12) =>
        TrialRecap.Create("  Strong start. Focus on form and 3x per week.  ", sessions, Now).Value;

    // ---- Trial recap ----

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(40)]
    public void Recap_accepts_package_sizes_in_steps_of_four(int sessions)
    {
        TrialRecap.Create("Plan", sessions, Now).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(44)]
    public void Recap_rejects_other_sizes(int sessions)
    {
        TrialRecap.Create("Plan", sessions, Now).Error.Should().Be(BillingErrors.TrialRecapInvalidSessions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Recap_needs_a_note(string? note)
    {
        TrialRecap.Create(note, 12, Now).Error.Should().Be(BillingErrors.TrialRecapInvalidNote);
    }

    [Fact]
    public void Recap_note_is_capped_and_trimmed()
    {
        TrialRecap.Create(new string('x', TrialRecap.MaxNoteLength + 1), 12, Now).IsFailure.Should().BeTrue();
        Recap().Note.Should().Be("Strong start. Focus on form and 3x per week.");
    }

    [Fact]
    public void Trial_coach_sends_the_recap_and_the_latest_one_wins()
    {
        var trial = Package(2, isTrial: true);

        trial.SendRecap(trial.TrainerId, Recap(8)).IsSuccess.Should().BeTrue();
        trial.SendRecap(trial.TrainerId, Recap(16)).IsSuccess.Should().BeTrue();

        trial.RecommendedSessions.Should().Be(16);
        trial.RecapNote.Should().StartWith("Strong start");
        trial.RecapSentAt.Should().Be(Now);
    }

    [Fact]
    public void Another_coach_cannot_send_the_recap()
    {
        var trial = Package(2, isTrial: true);

        trial.SendRecap(Guid.NewGuid(), Recap()).Error.Should().Be(BillingErrors.TrialRecapForbidden);
        trial.RecapSentAt.Should().BeNull();
    }

    [Fact]
    public void A_paid_package_has_no_recap()
    {
        var paid = Package(8, isTrial: false);

        paid.SendRecap(paid.TrainerId, Recap()).Error.Should().Be(BillingErrors.TrialRecapNotATrial);
    }

    // ---- Package interest ----

    [Fact]
    public void Lead_from_a_recapped_trial_suggests_the_coach_size()
    {
        var trial = Package(2, isTrial: true);
        trial.SendRecap(trial.TrainerId, Recap(12));

        var lead = PackageInterest.ForPackage(trial, Now);

        lead.Source.Should().Be(PackageInterestSource.TrialRecap);
        lead.RecommendedSessions.Should().Be(12);
        lead.TrainerId.Should().Be(trial.TrainerId);
        lead.Status.Should().Be(PackageInterestStatus.Open);
    }

    [Fact]
    public void Lead_from_a_trial_without_recap_is_a_follow_up_with_no_size()
    {
        var lead = PackageInterest.ForPackage(Package(2, isTrial: true), Now);

        lead.Source.Should().Be(PackageInterestSource.TrialFollowUp);
        lead.RecommendedSessions.Should().BeNull();
    }

    [Fact]
    public void Lead_from_a_paid_package_is_a_renewal_of_the_same_size()
    {
        var lead = PackageInterest.ForPackage(Package(8, isTrial: false), Now);

        lead.Source.Should().Be(PackageInterestSource.Renewal);
        lead.RecommendedSessions.Should().Be(8);
    }

    [Fact]
    public void Lead_is_converted_only_by_a_paid_package_bought_after_it()
    {
        var lead = PackageInterest.ForPackage(Package(2, isTrial: true), Now);

        lead.IsPendingGiven(new[] { Now.AddDays(-30) }).Should().BeTrue("an older package is not this sale");
        lead.IsConvertedBy(new[] { Now.AddHours(2) }).Should().BeTrue();
        lead.IsPendingGiven(new[] { Now.AddHours(2) }).Should().BeFalse();
    }

    [Fact]
    public void Desk_dismisses_a_lead_once()
    {
        var lead = PackageInterest.ForPackage(Package(2, isTrial: true), Now);
        var staff = Guid.NewGuid();

        lead.Dismiss(staff, Now).IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(PackageInterestStatus.Dismissed);
        lead.DismissedBy.Should().Be(staff);
        lead.IsPendingGiven(Array.Empty<DateTime>()).Should().BeFalse();
        lead.Dismiss(staff, Now).Error.Should().Be(BillingErrors.PackageInterestNotOpen);
    }

    // ---- Follow-up milestones ----

    [Theory]
    [InlineData(PackageMilestone.TrialFollowUpDay3)]
    [InlineData(PackageMilestone.TrialLastChanceDay12)]
    public void Follow_up_fires_once_on_a_finished_trial(PackageMilestone followUp)
    {
        var trial = FinishedTrial();

        trial.TryMarkFollowUp(followUp).Should().BeTrue();
        trial.TryMarkFollowUp(followUp).Should().BeFalse();

        trial.GetDomainEvents().OfType<SessionPackageMilestoneReachedDomainEvent>()
            .Should().ContainSingle(e => e.Milestone == followUp);
    }

    [Fact]
    public void Follow_up_needs_a_finished_trial()
    {
        Package(2, isTrial: true).TryMarkFollowUp(PackageMilestone.TrialFollowUpDay3).Should().BeFalse("trial still running");
        Package(8, isTrial: false).TryMarkFollowUp(PackageMilestone.TrialFollowUpDay3).Should().BeFalse("not a trial");
    }

    [Fact]
    public void Only_follow_up_milestones_can_be_marked()
    {
        FinishedTrial().TryMarkFollowUp(PackageMilestone.RenewalOneLeft).Should().BeFalse();
    }
}

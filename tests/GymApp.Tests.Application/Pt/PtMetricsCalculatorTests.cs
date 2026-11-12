using GymApp.Modules.Billing.Application.Services;

namespace GymApp.Tests.Application.Pt;

public class PtMetricsCalculatorTests
{
    private static readonly DateTime From = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CoachA = Guid.NewGuid();
    private static readonly Guid CoachB = Guid.NewGuid();

    private static PtPackageFact Trial(Guid member, Guid coach, DateTime start, DateTime? finished) =>
        new(Guid.NewGuid(), member, coach, IsTrial: true, start, finished);

    private static PtPackageFact Paid(Guid member, Guid coach, DateTime bought) =>
        new(Guid.NewGuid(), member, coach, IsTrial: false, bought, null);

    private static PtCoachMetrics For(IReadOnlyList<PtCoachMetrics> rows, Guid coach) => rows.Single(r => r.TrainerId == coach);

    [Fact]
    public void Conversion_is_credited_to_the_trial_coach_even_if_another_coach_sells_the_package()
    {
        var member = Guid.NewGuid();
        var trial = Trial(member, CoachA, From.AddDays(1), From.AddDays(5));
        var packages = new[] { trial, Paid(member, CoachB, From.AddDays(10)) };

        var rows = PtMetricsCalculator.ByCoach(packages, Array.Empty<PtUsageFact>(), From, To);

        For(rows, CoachA).TrialsFinished.Should().Be(1);
        For(rows, CoachA).TrialsConverted.Should().Be(1);
        For(rows, CoachA).ConversionRate.Should().Be(100m);
        For(rows, CoachB).TrialsConverted.Should().Be(0);
        For(rows, CoachB).NewPaidPackages.Should().Be(1);
    }

    [Fact]
    public void Purchase_after_the_14_day_window_is_not_a_conversion()
    {
        var member = Guid.NewGuid();
        var finished = From.AddDays(2);
        var packages = new[] { Trial(member, CoachA, From, finished), Paid(member, CoachA, finished.AddDays(15)) };

        var rows = PtMetricsCalculator.ByCoach(packages, Array.Empty<PtUsageFact>(), From, To);

        For(rows, CoachA).TrialsConverted.Should().Be(0);
        For(rows, CoachA).ConversionRate.Should().Be(0m);
    }

    [Fact]
    public void Buying_before_the_trial_ends_counts_as_converted()
    {
        var member = Guid.NewGuid();
        var packages = new[] { Trial(member, CoachA, From, From.AddDays(6)), Paid(member, CoachA, From.AddDays(3)) };

        For(PtMetricsCalculator.ByCoach(packages, Array.Empty<PtUsageFact>(), From, To), CoachA)
            .TrialsConverted.Should().Be(1);
    }

    [Fact]
    public void Conversion_rate_is_per_coach()
    {
        var converted = Guid.NewGuid();
        var lost1 = Guid.NewGuid();
        var lost2 = Guid.NewGuid();
        var packages = new[]
        {
            Trial(converted, CoachA, From, From.AddDays(1)),
            Paid(converted, CoachA, From.AddDays(2)),
            Trial(lost1, CoachA, From, From.AddDays(1)),
            Trial(lost2, CoachA, From, From.AddDays(1)),
        };

        For(PtMetricsCalculator.ByCoach(packages, Array.Empty<PtUsageFact>(), From, To), CoachA)
            .ConversionRate.Should().Be(33.3m);
    }

    [Fact]
    public void Second_paid_package_is_a_renewal()
    {
        var member = Guid.NewGuid();
        var packages = new[] { Paid(member, CoachA, From.AddDays(-20)), Paid(member, CoachA, From.AddDays(4)) };

        var row = For(PtMetricsCalculator.ByCoach(packages, Array.Empty<PtUsageFact>(), From, To), CoachA);

        row.Renewals.Should().Be(1);
        row.NewPaidPackages.Should().Be(0);
    }

    [Fact]
    public void Sessions_logged_count_the_period_and_subtract_reversals()
    {
        var package = Paid(Guid.NewGuid(), CoachA, From.AddDays(-30));
        var reversedSession = Guid.NewGuid();
        var usages = new[]
        {
            new PtUsageFact(package.PackageId, Guid.NewGuid(), From.AddDays(-1), false), // before period
            new PtUsageFact(package.PackageId, Guid.NewGuid(), From.AddDays(1), false),
            new PtUsageFact(package.PackageId, reversedSession, From.AddDays(2), false),
            new PtUsageFact(package.PackageId, reversedSession, From.AddDays(3), true),
            new PtUsageFact(package.PackageId, Guid.NewGuid(), From.AddDays(4), false),
        };

        For(PtMetricsCalculator.ByCoach(new[] { package }, usages, From, To), CoachA).SessionsLogged.Should().Be(2);
    }

    [Fact]
    public void Not_converted_lists_finished_trials_without_any_later_paid_package()
    {
        var now = From.AddDays(20);
        var lost = Guid.NewGuid();
        var won = Guid.NewGuid();
        var stillOnTrial = Guid.NewGuid();
        var packages = new[]
        {
            Trial(lost, CoachA, From, From.AddDays(2)),
            Trial(won, CoachA, From, From.AddDays(2)),
            Paid(won, CoachA, From.AddDays(3)),
            Trial(stillOnTrial, CoachA, From, finished: null),
        };

        var rows = PtMetricsCalculator.NotConverted(packages, now);

        rows.Should().ContainSingle();
        rows[0].MemberId.Should().Be(lost);
        rows[0].DaysSinceFinished.Should().Be(18);
        rows[0].ConversionWindowClosed.Should().BeTrue();
    }

    [Fact]
    public void Paid_package_bought_before_the_trial_does_not_convert_it()
    {
        var member = Guid.NewGuid();
        var packages = new[] { Paid(member, CoachA, From.AddDays(-60)), Trial(member, CoachA, From, From.AddDays(2)) };

        PtMetricsCalculator.NotConverted(packages, From.AddDays(5)).Should().ContainSingle();
    }
}

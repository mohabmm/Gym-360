namespace GymApp.Modules.Billing.Application.Services;

/// <summary>A package as the PT metrics see it.</summary>
public sealed record PtPackageFact(
    Guid PackageId,
    Guid MemberId,
    Guid TrainerId,
    bool IsTrial,
    DateTime CreatedAtUtc,
    DateTime? ExhaustedAtUtc);

/// <summary>One counted (or reversed) session on a package.</summary>
public sealed record PtUsageFact(Guid PackageId, Guid SessionId, DateTime AtUtc, bool IsReversal);

public sealed record PtCoachMetrics(
    Guid TrainerId,
    int TrialsFinished,
    int TrialsConverted,
    decimal ConversionRate,
    int Renewals,
    int NewPaidPackages,
    int SessionsLogged);

public sealed record PtNotConvertedTrial(
    Guid PackageId,
    Guid MemberId,
    Guid TrainerId,
    DateTime TrialStartedAtUtc,
    DateTime FinishedAtUtc,
    int DaysSinceFinished,
    bool ConversionWindowClosed);

/// <summary>
/// Pure PT business rules (pt.owner_dashboard, pt.trial_end_alerts):
/// <list type="bullet">
/// <item><b>Trial finished</b>: a trial package whose last free session was used (ExhaustedAt).</item>
/// <item><b>Converted</b>: the trainee bought a paid package with any coach after the trial
/// started and no later than <see cref="ConversionWindow"/> after it finished. Credited to the trial coach.</item>
/// <item><b>Renewal</b>: a paid package bought by a trainee who already had a paid package.</item>
/// <item><b>Sessions logged</b>: counted sessions in the period, minus reversals.</item>
/// </list>
/// </summary>
public static class PtMetricsCalculator
{
    public static readonly TimeSpan ConversionWindow = TimeSpan.FromDays(14);

    public static IReadOnlyList<PtCoachMetrics> ByCoach(
        IReadOnlyCollection<PtPackageFact> packages,
        IReadOnlyCollection<PtUsageFact> usages,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var paidByMember = PaidPackagesByMember(packages);
        var finishedTrials = packages
            .Where(p => p.IsTrial && p.ExhaustedAtUtc is { } end && end >= fromUtc && end < toUtc)
            .ToList();
        var paidInPeriod = packages.Where(p => !p.IsTrial && p.CreatedAtUtc >= fromUtc && p.CreatedAtUtc < toUtc).ToList();
        var trainerOfPackage = packages.ToDictionary(p => p.PackageId, p => p.TrainerId);
        var usageInPeriod = usages.Where(u => u.AtUtc >= fromUtc && u.AtUtc < toUtc).ToList();

        var coaches = finishedTrials.Select(p => p.TrainerId)
            .Concat(paidInPeriod.Select(p => p.TrainerId))
            .Concat(usageInPeriod.Select(u => trainerOfPackage.GetValueOrDefault(u.PackageId)))
            .Where(id => id != Guid.Empty)
            .Distinct();

        return coaches
            .Select(coach =>
            {
                var trials = finishedTrials.Where(t => t.TrainerId == coach).ToList();
                var converted = trials.Count(t => IsConverted(t, paidByMember));
                var paid = paidInPeriod.Where(p => p.TrainerId == coach).ToList();
                var renewals = paid.Count(p => IsRenewal(p, paidByMember));
                var sessions = usageInPeriod
                    .Where(u => trainerOfPackage.GetValueOrDefault(u.PackageId) == coach)
                    .Sum(u => u.IsReversal ? -1 : 1);

                return new PtCoachMetrics(
                    coach,
                    trials.Count,
                    converted,
                    Rate(converted, trials.Count),
                    renewals,
                    paid.Count - renewals,
                    Math.Max(0, sessions));
            })
            .OrderByDescending(m => m.SessionsLogged)
            .ToList();
    }

    /// <summary>Finished trials whose trainee has not bought a paid package (desk follow-up list).</summary>
    public static IReadOnlyList<PtNotConvertedTrial> NotConverted(IReadOnlyCollection<PtPackageFact> packages, DateTime nowUtc)
    {
        var paidByMember = PaidPackagesByMember(packages);

        return packages
            .Where(p => p.IsTrial && p.ExhaustedAtUtc.HasValue && !IsConverted(p, paidByMember, ignoreWindow: true))
            .Select(p => new PtNotConvertedTrial(
                p.PackageId,
                p.MemberId,
                p.TrainerId,
                p.CreatedAtUtc,
                p.ExhaustedAtUtc!.Value,
                (int)Math.Floor((nowUtc - p.ExhaustedAtUtc.Value).TotalDays),
                nowUtc > p.ExhaustedAtUtc.Value + ConversionWindow))
            .OrderByDescending(t => t.FinishedAtUtc)
            .ToList();
    }

    public static bool IsConverted(
        PtPackageFact trial,
        IReadOnlyDictionary<Guid, List<PtPackageFact>> paidByMember,
        bool ignoreWindow = false)
    {
        if (!paidByMember.TryGetValue(trial.MemberId, out var paid))
            return false;

        var deadline = trial.ExhaustedAtUtc is { } end && !ignoreWindow ? end + ConversionWindow : DateTime.MaxValue;
        return paid.Any(p => p.CreatedAtUtc >= trial.CreatedAtUtc && p.CreatedAtUtc <= deadline);
    }

    private static bool IsRenewal(PtPackageFact package, IReadOnlyDictionary<Guid, List<PtPackageFact>> paidByMember) =>
        paidByMember.TryGetValue(package.MemberId, out var paid)
        && paid.Any(p => p.PackageId != package.PackageId && p.CreatedAtUtc < package.CreatedAtUtc);

    private static Dictionary<Guid, List<PtPackageFact>> PaidPackagesByMember(IEnumerable<PtPackageFact> packages) =>
        packages.Where(p => !p.IsTrial)
            .GroupBy(p => p.MemberId)
            .ToDictionary(g => g.Key, g => g.ToList());

    private static decimal Rate(int part, int whole) =>
        whole == 0 ? 0m : Math.Round(part * 100m / whole, 1);
}

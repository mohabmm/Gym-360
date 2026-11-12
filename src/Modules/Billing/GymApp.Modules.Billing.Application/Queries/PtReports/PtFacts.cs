using GymApp.Modules.Billing.Application.Services;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;

namespace GymApp.Modules.Billing.Application.Queries.PtReports;

internal static class PtFacts
{
    public static List<PtPackageFact> Packages(IEnumerable<SessionPackage> packages) =>
        packages.Select(p => new PtPackageFact(p.Id, p.MemberId, p.TrainerId, p.IsTrial, p.CreatedAt, p.ExhaustedAt)).ToList();

    public static List<PtUsageFact> Usages(IEnumerable<SessionPackage> packages) =>
        packages.SelectMany(p => p.UsageHistory
                .Where(u => u.UsageType is PackageUsageType.Completed or PackageUsageType.Reversed)
                .Select(u => new PtUsageFact(p.Id, u.SessionId, u.UsedAt, u.UsageType == PackageUsageType.Reversed)))
            .ToList();
}

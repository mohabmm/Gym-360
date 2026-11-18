using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

/// <summary>The member's lead that still waits for the desk (open and not converted), if any.</summary>
internal static class PendingPackageInterest
{
    public static async Task<PackageInterest?> FindAsync(
        IPackageInterestRepository interests,
        ISessionPackageRepository packages,
        PackageOwner owner,
        CancellationToken cancellationToken)
    {
        var leads = await interests.GetByMemberAsync(owner, cancellationToken);
        if (leads.Count == 0)
            return null;

        var paidCreatedAts = await PaidPackageDates.ForMemberAsync(packages, owner, cancellationToken);
        return leads.FirstOrDefault(i => i.IsPendingGiven(paidCreatedAts));
    }
}

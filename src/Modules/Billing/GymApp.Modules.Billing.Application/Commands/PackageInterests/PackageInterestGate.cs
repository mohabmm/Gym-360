using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

/// <summary>pt.package_interest needs the PT packages it sells.</summary>
internal static class PackageInterestGate
{
    private static readonly string[] Required = { FeatureKeys.PtPackages, FeatureKeys.PtPackageInterest };

    public static async Task<Result> CheckAsync(IFeatureFlagService flags, Guid gymId, CancellationToken cancellationToken)
    {
        foreach (var key in Required)
        {
            if (!await flags.IsEnabledAsync(gymId, key, cancellationToken))
                return Result.Failure(FeatureFlagErrors.FeatureDisabled(key));
        }

        return Result.Success();
    }
}

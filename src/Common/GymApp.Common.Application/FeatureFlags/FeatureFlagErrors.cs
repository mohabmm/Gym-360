using GymApp.Common.Domain;

namespace GymApp.Common.Application.FeatureFlags;

public static class FeatureFlagErrors
{
    public const string FeatureDisabledPrefix = "FEATURE_DISABLED:";

    /// <summary>403 returned when a disabled feature is called. Clients refresh flags and hide it.</summary>
    public static Error FeatureDisabled(string featureKey) =>
        new($"{FeatureDisabledPrefix}{featureKey}",
            $"The '{featureKey}' feature is not enabled for this gym.",
            new { feature = featureKey });

    public static readonly Error UnknownFeature =
        new("FeatureFlags.UnknownFeature", "Unknown feature key.");

    public static readonly Error UnknownPreset =
        new("FeatureFlags.PresetNotFound", "Unknown preset.");

    public static readonly Error GymNotFound =
        new("FeatureFlags.GymNotFound", "Gym not found.");
}

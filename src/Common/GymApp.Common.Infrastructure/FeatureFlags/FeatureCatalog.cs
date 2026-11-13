using GymApp.Common.Application.FeatureFlags;

namespace GymApp.Common.Infrastructure.FeatureFlags;

internal static class FeatureCatalog
{
    public static FeatureCatalogItem ToItem(FeatureDefinition def) => new(
        def.Key,
        def.Module,
        def.Kind == FeatureKind.Setting ? "setting" : "feature",
        def.Text.NameEn,
        def.Text.NameAr,
        def.Text.DescriptionEn,
        def.Text.DescriptionAr,
        AppNames(def.Apps));

    public static IReadOnlyList<string> AppNames(FeatureApps apps) =>
        Enum.GetValues<FeatureApps>()
            .Where(a => a is not FeatureApps.None and not FeatureApps.All && apps.HasFlag(a))
            .Select(a => a switch
            {
                FeatureApps.Trainee => "trainee",
                FeatureApps.Trainer => "trainer",
                FeatureApps.StaffMobile => "staff_mobile",
                _ => "staff_web",
            })
            .ToList();
}

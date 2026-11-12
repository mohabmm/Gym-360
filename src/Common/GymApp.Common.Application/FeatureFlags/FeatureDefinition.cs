namespace GymApp.Common.Application.FeatureFlags;

/// <summary>Client apps a feature shows up in (used by the dashboard to explain impact).</summary>
[Flags]
public enum FeatureApps
{
    None = 0,
    Trainee = 1,
    Trainer = 2,
    StaffMobile = 4,
    StaffWeb = 8,
    All = Trainee | Trainer | StaffMobile | StaffWeb,
}

/// <summary>A feature toggles a module on/off; a setting is a per-gym behaviour switch.</summary>
public enum FeatureKind
{
    Feature = 0,
    Setting = 1,
}

/// <summary>Localized display text for a flag.</summary>
public sealed record FeatureText(string NameEn, string NameAr, string DescriptionEn, string DescriptionAr);

/// <summary>
/// One entry of the code-defined <see cref="FeatureRegistry"/>. Seeded to the database on startup,
/// so adding a definition here makes it appear in the Super Admin dashboard automatically.
/// </summary>
/// <param name="Key">Stable key, e.g. <c>pt.packages</c>.</param>
/// <param name="Module">Grouping shown in the dashboard, e.g. <c>pt</c>, <c>gym</c>, <c>training</c>.</param>
/// <param name="Text">English and Arabic name and description.</param>
/// <param name="Apps">Apps whose UI the flag changes.</param>
/// <param name="InPtModule">Whether the <c>pt_module</c> preset turns it on when the preset is first seeded.</param>
/// <param name="Kind">Feature or setting.</param>
public sealed record FeatureDefinition(
    string Key,
    string Module,
    FeatureText Text,
    FeatureApps Apps,
    bool InPtModule,
    FeatureKind Kind = FeatureKind.Feature)
{
    /// <summary>Value the <c>business</c> preset gets when first seeded: every feature on, settings at their pt default.</summary>
    public bool InBusiness => Kind == FeatureKind.Feature || InPtModule;
}

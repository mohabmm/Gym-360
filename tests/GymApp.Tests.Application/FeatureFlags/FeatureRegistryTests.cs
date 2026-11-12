using GymApp.Common.Application.FeatureFlags;

namespace GymApp.Tests.Application.FeatureFlags;

public class FeatureRegistryTests
{
    private static readonly string[] PtModuleOn =
    {
        FeatureKeys.PtTrialSessions,
        FeatureKeys.PtProgressMeasurements,
        FeatureKeys.PtTrialEndAlerts,
        FeatureKeys.PtPackages,
        FeatureKeys.PtDualSessionLogging,
        FeatureKeys.PtRenewalReminders,
        FeatureKeys.PtOwnerDashboard,
    };

    [Fact]
    public void Keys_are_unique()
    {
        FeatureRegistry.All.Select(d => d.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Pt_module_turns_on_exactly_the_seven_pt_features()
    {
        FeatureRegistry.PtModuleDefaults.Should().BeEquivalentTo(PtModuleOn);
    }

    [Theory]
    [InlineData(FeatureKeys.PtMonthlyReport)]
    [InlineData(FeatureKeys.PtCoachRatings)]
    [InlineData(FeatureKeys.PtCoachPerformance)]
    public void Later_pt_features_are_off_in_pt_module(string key)
    {
        FeatureRegistry.Find(key)!.InPtModule.Should().BeFalse();
    }

    [Fact]
    public void Auto_expiry_is_a_setting_that_defaults_off_in_both_presets()
    {
        var def = FeatureRegistry.Find(FeatureKeys.PtAutoExpiry)!;

        def.Kind.Should().Be(FeatureKind.Setting);
        def.InPtModule.Should().BeFalse();
        def.InBusiness.Should().BeFalse("settings keep their default even in the all-features preset");
    }

    [Fact]
    public void Business_preset_turns_on_every_feature()
    {
        FeatureRegistry.All
            .Where(d => d.Kind == FeatureKind.Feature)
            .Should().OnlyContain(d => d.InBusiness);
    }

    [Fact]
    public void Every_key_constant_is_registered()
    {
        var constants = typeof(FeatureKeys).GetFields()
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);

        constants.Should().OnlyContain(k => FeatureRegistry.IsKnown(k));
    }

    [Fact]
    public void Every_flag_has_english_and_arabic_text()
    {
        FeatureRegistry.All.Should().OnlyContain(d =>
            !string.IsNullOrWhiteSpace(d.Text.NameEn) && !string.IsNullOrWhiteSpace(d.Text.NameAr)
            && !string.IsNullOrWhiteSpace(d.Text.DescriptionEn) && !string.IsNullOrWhiteSpace(d.Text.DescriptionAr));
    }

    [Theory]
    [InlineData("session_packages", FeatureKeys.PtPackages)]
    [InlineData("progress_tracking", FeatureKeys.PtProgressMeasurements)]
    [InlineData("class_management", FeatureKeys.Classes)]
    [InlineData("nutrition_tracking", FeatureKeys.Nutrition)]
    public void Legacy_plan_codes_map_to_flags(string legacyCode, string expectedKey)
    {
        var (kind, key) = LegacyFeatureMap.Lookup(legacyCode);

        kind.Should().Be(LegacyFeatureMap.Kind.Flag);
        key.Should().Be(expectedKey);
    }

    [Fact]
    public void Core_legacy_codes_are_always_on_and_testing_mode_is_unmapped()
    {
        LegacyFeatureMap.Lookup("member_management").Kind.Should().Be(LegacyFeatureMap.Kind.AlwaysOn);
        LegacyFeatureMap.Lookup("early_session_start").Kind.Should().Be(LegacyFeatureMap.Kind.Unmapped);
    }
}

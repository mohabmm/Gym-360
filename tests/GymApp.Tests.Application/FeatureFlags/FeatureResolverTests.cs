using GymApp.Common.Application.FeatureFlags;

namespace GymApp.Tests.Application.FeatureFlags;

public class FeatureResolverTests
{
    private static readonly Dictionary<string, bool> None = new();

    [Fact]
    public void Override_wins_over_preset()
    {
        var preset = new Dictionary<string, bool> { ["classes"] = false };
        var overrides = new Dictionary<string, bool> { ["classes"] = true };

        var flag = FeatureResolver.ResolveOne("classes", preset, overrides);

        flag.Enabled.Should().BeTrue();
        flag.Source.Should().Be(FlagSource.Override);
        flag.PresetValue.Should().BeFalse();
    }

    [Fact]
    public void Override_can_turn_off_a_preset_feature()
    {
        var preset = new Dictionary<string, bool> { ["pt.packages"] = true };
        var overrides = new Dictionary<string, bool> { ["pt.packages"] = false };

        var flag = FeatureResolver.ResolveOne("pt.packages", preset, overrides);

        flag.Enabled.Should().BeFalse();
        flag.Source.Should().Be(FlagSource.Override);
        flag.PresetValue.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Preset_applies_when_there_is_no_override(bool presetValue)
    {
        var preset = new Dictionary<string, bool> { ["nutrition"] = presetValue };

        var flag = FeatureResolver.ResolveOne("nutrition", preset, None);

        flag.Enabled.Should().Be(presetValue);
        flag.Source.Should().Be(FlagSource.Preset);
    }

    [Fact]
    public void Missing_from_preset_and_overrides_is_off()
    {
        var flag = FeatureResolver.ResolveOne("brand.new.flag", None, None);

        flag.Enabled.Should().BeFalse();
        flag.Source.Should().Be(FlagSource.Default);
    }

    [Fact]
    public void Resolve_returns_every_registry_key_exactly_once()
    {
        var keys = FeatureRegistry.All.Select(d => d.Key).ToList();

        var flags = FeatureResolver.Resolve(keys, None, None);

        flags.Select(f => f.Key).Should().Equal(keys);
        flags.Should().OnlyContain(f => !f.Enabled, "no preset and no override means everything is OFF");
    }

    [Fact]
    public void GymFeatureFlags_treats_unknown_keys_as_off()
    {
        var flags = new GymFeatureFlags(
            Guid.NewGuid(),
            FeatureRegistry.PtModulePreset,
            new[] { new ResolvedFlag("pt.packages", true, FlagSource.Preset, true) },
            DateTime.UtcNow);

        flags.IsEnabled("pt.packages").Should().BeTrue();
        flags.IsEnabled("not.a.flag").Should().BeFalse();
    }
}

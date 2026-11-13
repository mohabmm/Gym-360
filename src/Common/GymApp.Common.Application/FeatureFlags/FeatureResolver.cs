namespace GymApp.Common.Application.FeatureFlags;

/// <summary>Where a gym's effective flag value came from.</summary>
public enum FlagSource
{
    /// <summary>No preset entry and no override: fail-closed OFF.</summary>
    Default = 0,
    Preset = 1,
    Override = 2,
}

/// <summary>Effective value of one flag for one gym.</summary>
public sealed record ResolvedFlag(string Key, bool Enabled, FlagSource Source, bool PresetValue);

/// <summary>
/// Pure resolution rule shared by every flag provider: <b>override &gt; preset &gt; OFF</b>.
/// Only keys known to the registry are resolved; unknown keys are always OFF.
/// </summary>
public static class FeatureResolver
{
    public static IReadOnlyList<ResolvedFlag> Resolve(
        IEnumerable<string> registryKeys,
        IReadOnlyDictionary<string, bool> presetValues,
        IReadOnlyDictionary<string, bool> overrides)
    {
        return registryKeys
            .Select(key => ResolveOne(key, presetValues, overrides))
            .ToList();
    }

    public static ResolvedFlag ResolveOne(
        string key,
        IReadOnlyDictionary<string, bool> presetValues,
        IReadOnlyDictionary<string, bool> overrides)
    {
        var hasPreset = presetValues.TryGetValue(key, out var presetValue);

        if (overrides.TryGetValue(key, out var overrideValue))
            return new ResolvedFlag(key, overrideValue, FlagSource.Override, hasPreset && presetValue);

        return hasPreset
            ? new ResolvedFlag(key, presetValue, FlagSource.Preset, presetValue)
            : new ResolvedFlag(key, false, FlagSource.Default, false);
    }
}

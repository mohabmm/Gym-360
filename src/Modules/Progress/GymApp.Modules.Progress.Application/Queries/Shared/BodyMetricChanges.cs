using GymApp.Modules.Progress.Domain.Entities;

namespace GymApp.Modules.Progress.Application.Queries.Shared;

/// <summary>Change in weight and body fat between a user's first and latest body-metric entries.</summary>
internal sealed record BodyMetricChanges(decimal? WeightChange, decimal? BodyFatChange)
{
    private static readonly BodyMetricChanges None = new(null, null);

    /// <param name="entriesOldestFirst">Entries ordered by date ascending.</param>
    public static BodyMetricChanges Between(IReadOnlyList<BodyMetricEntry> entriesOldestFirst)
    {
        if (entriesOldestFirst.Count < 2)
            return None;

        var first = entriesOldestFirst[0];
        var last = entriesOldestFirst[^1];

        // Lifted nullable arithmetic: null unless both sides have a value.
        return new BodyMetricChanges(
            last.Weight - first.Weight,
            last.BodyFatPercentage - first.BodyFatPercentage);
    }
}

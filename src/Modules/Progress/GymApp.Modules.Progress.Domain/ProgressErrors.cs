using GymApp.Common.Domain;

namespace GymApp.Modules.Progress.Domain;

public static class ProgressErrors
{
    public static readonly Error MetricEntryNotFound = new(
        "Progress.MetricEntryNotFound",
        "The body metric entry was not found.");

    public static readonly Error PhotoNotFound = new(
        "Progress.PhotoNotFound",
        "The progress photo was not found.");

    public static readonly Error StreakNotFound = new(
        "Progress.StreakNotFound",
        "The streak was not found.");

    public static readonly Error DuplicateMetricEntry = new(
        "Progress.DuplicateMetricEntry",
        "A body metric entry already exists for this user on this date.");

    public static readonly Error PhotoLimitReached = new(
        "Progress.PhotoLimitReached",
        "The photo storage limit has been reached.");

    public static readonly Error InvalidBodyFatPercentage = new(
        "Progress.InvalidBodyFatPercentage",
        "Body fat percentage must be between 0 and 100.");

    public static readonly Error InvalidWeight = new(
        "Progress.InvalidWeight",
        "Weight must be greater than zero.");
}

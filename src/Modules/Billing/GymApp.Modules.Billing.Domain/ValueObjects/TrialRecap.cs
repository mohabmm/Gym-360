using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Errors;

namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>
/// pt.trial_recap: the coach's short plan for a trainee after the free trial. The coach only
/// recommends a package size; the desk prices it (trainers never set prices).
/// </summary>
public sealed record TrialRecap
{
    public const int MaxNoteLength = 500;
    public const int SessionStep = 4;
    public const int MinSessions = 4;
    public const int MaxSessions = 40;

    private TrialRecap(string note, int recommendedSessions, DateTime sentAtUtc)
    {
        Note = note;
        RecommendedSessions = recommendedSessions;
        SentAtUtc = sentAtUtc;
    }

    public string Note { get; }
    public int RecommendedSessions { get; }
    public DateTime SentAtUtc { get; }

    /// <summary>Same size rule as quick-assign: a multiple of 4 between 4 and 40.</summary>
    public static Result<TrialRecap> Create(string? note, int recommendedSessions, DateTime sentAtUtc)
    {
        var trimmed = note?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxNoteLength)
            return Result.Failure<TrialRecap>(BillingErrors.TrialRecapInvalidNote);

        if (!IsValidSize(recommendedSessions))
            return Result.Failure<TrialRecap>(BillingErrors.TrialRecapInvalidSessions);

        return Result.Success(new TrialRecap(trimmed, recommendedSessions, sentAtUtc));
    }

    public static bool IsValidSize(int sessions) =>
        sessions is >= MinSessions and <= MaxSessions && sessions % SessionStep == 0;
}

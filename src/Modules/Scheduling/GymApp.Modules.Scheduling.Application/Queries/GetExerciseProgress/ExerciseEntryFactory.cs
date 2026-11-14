using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>Turns one exercise log into a board entry with its derived numbers.</summary>
internal static class ExerciseEntryFactory
{
    public static ExerciseEntryDto Create(SessionExerciseLog log, ProgressSession session)
    {
        var perSet = RepsParser.PerSet(log.Reps, log.Sets);
        var totalReps = perSet.Sum();

        return new ExerciseEntryDto(
            session.Session.Id,
            session.Session.ScheduledDate,
            session.Number,
            session.IsTrial,
            session.Session.DayLabel,
            log.Sets,
            log.Reps,
            totalReps,
            log.Weight,
            log.Weight is { } weight ? weight * totalReps : null,
            EstimateOneRepMax(log.Weight, perSet),
            log.Notes,
            SessionExerciseLogMapper.TargetOf(log));
    }

    // Epley: weight × (1 + reps / 30), from the set with the most reps at the working weight.
    private static decimal? EstimateOneRepMax(decimal? weight, IReadOnlyList<int> perSet)
    {
        if (weight is not > 0m || perSet.Count == 0)
            return null;

        var w = weight.Value;
        var reps = perSet.Max();
        var estimate = reps == 1 ? w : w * (1 + reps / 30m);
        return Math.Round(estimate * 2, MidpointRounding.AwayFromZero) / 2;
    }
}

/// <summary>A completed session as numbered on the member's board.</summary>
internal sealed record ProgressSession(Session Session, int Number, bool IsTrial);

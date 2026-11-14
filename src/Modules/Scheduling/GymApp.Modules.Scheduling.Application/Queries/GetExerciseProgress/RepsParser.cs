namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>
/// Reads the coach's reps text. The trainer app stores per-set reps as CSV ("10,8,8"); a single
/// value ("10" or a range "8-12", read as its lower bound) means the same reps for every set.
/// </summary>
internal static class RepsParser
{
    public static IReadOnlyList<int> PerSet(string reps, int sets)
    {
        var values = reps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(LeadingNumber)
            .Where(v => v > 0)
            .ToList();

        if (values.Count == 1 && sets > 1)
            return Enumerable.Repeat(values[0], sets).ToList();

        return values;
    }

    public static int? FirstOrNull(string? reps)
    {
        if (string.IsNullOrWhiteSpace(reps))
            return null;

        var value = LeadingNumber(reps.Trim());
        return value > 0 ? value : null;
    }

    private static int LeadingNumber(string text)
    {
        var digits = new string(text.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : 0;
    }
}

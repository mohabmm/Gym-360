namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

/// <summary>Gym-local dates. All gyms are in Egypt (MVP), so the zone is fixed to Africa/Cairo.</summary>
internal static class TrainingCalendar
{
    private static readonly TimeZoneInfo Egypt = Resolve();

    public static DateOnly LocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Egypt));

    public static DateOnly Today() => LocalDate(DateTime.UtcNow);

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
        }
        throw new InvalidOperationException("Egypt time zone is not available on this host.");
    }
}

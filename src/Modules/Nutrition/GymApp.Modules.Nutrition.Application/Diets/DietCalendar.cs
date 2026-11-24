namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>Gym-local dates. All gyms are in Egypt (MVP), so the zone is fixed to Africa/Cairo.</summary>
internal static class DietCalendar
{
    private static readonly TimeZoneInfo Egypt = Resolve();

    public static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Egypt));

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

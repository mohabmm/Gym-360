namespace GymApp.Modules.Scheduling.Application.Abstractions;

/// <summary>
/// Converts between UTC and gym-local wall-clock time. Sessions store
/// ScheduledDate/ScheduledTime as the gym's local wall-clock, so any comparison
/// against "now" must go through here rather than treating the stored value as UTC.
/// All gyms are in Egypt (MVP), so the zone is fixed to Africa/Cairo.
/// </summary>
internal static class GymLocalTime
{
    private static readonly TimeZoneInfo EgyptTimeZone = ResolveEgyptTimeZone();

    /// <summary>Gym-local wall-clock for the given UTC instant.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), EgyptTimeZone);

    /// <summary>
    /// The gym's calendar date at the given UTC instant. Use this — not the UTC date —
    /// wherever "today" is compared with a session's (gym-local) ScheduledDate: between
    /// midnight and 02:00/03:00 Cairo time the UTC date is still yesterday.
    /// </summary>
    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(FromUtc(utcNow));

    /// <summary>The gym's calendar date right now.</summary>
    public static DateOnly Today() => Today(DateTime.UtcNow);

    /// <summary>UTC instant for the given gym-local wall-clock value.</summary>
    public static DateTime ToUtc(DateTime gymLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(gymLocal, DateTimeKind.Unspecified), EgyptTimeZone);

    private static TimeZoneInfo ResolveEgyptTimeZone()
    {
        // .NET 8 accepts both IANA and Windows IDs via the TZ data, but fall back
        // just in case the host OS only ships one family.
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        throw new InvalidOperationException("Egypt time zone is not available on this host.");
    }
}

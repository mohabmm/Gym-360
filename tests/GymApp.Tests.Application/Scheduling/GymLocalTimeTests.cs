using System.Globalization;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Tests.Application.Scheduling;

/// <summary>
/// "Today" for scheduling is the gym's (Africa/Cairo) calendar date. Deriving it from UTC
/// was wrong between local midnight and 02:00 (winter) / 03:00 (summer): the UTC date is
/// still yesterday, so yesterday's sessions counted as upcoming and yesterday could be booked.
/// </summary>
public class GymLocalTimeTests
{
    [Theory]
    // Summer time (UTC+3)
    [InlineData("2026-07-14T20:59:00Z", "2026-07-14")] // 23:59 Cairo
    [InlineData("2026-07-14T21:00:00Z", "2026-07-15")] // 00:00 Cairo — already tomorrow locally
    [InlineData("2026-07-14T23:30:00Z", "2026-07-15")] // 02:30 Cairo
    [InlineData("2026-07-15T00:00:00Z", "2026-07-15")]
    // Winter time (UTC+2)
    [InlineData("2026-01-14T21:59:00Z", "2026-01-14")] // 23:59 Cairo
    [InlineData("2026-01-14T22:00:00Z", "2026-01-15")] // 00:00 Cairo
    [InlineData("2026-01-14T12:00:00Z", "2026-01-14")]
    public void Today_IsTheCairoCalendarDate(string utcInstant, string expectedCairoDate)
    {
        var utc = DateTime.Parse(
            utcInstant, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        GymLocalTime.Today(utc).Should().Be(DateOnly.Parse(expectedCairoDate, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FromUtc_And_ToUtc_RoundTrip()
    {
        var utc = new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc);

        var local = GymLocalTime.FromUtc(utc);

        local.Should().Be(new DateTime(2026, 10, 12, 21, 0, 0));
        GymLocalTime.ToUtc(local).Should().Be(utc);
    }

    [Fact]
    public void Today_AcceptsUnspecifiedKindAsUtc()
    {
        // IDateTimeProvider fakes and EF-materialised values often carry Kind=Unspecified.
        var unspecified = new DateTime(2026, 7, 14, 21, 30, 0, DateTimeKind.Unspecified);

        GymLocalTime.Today(unspecified).Should().Be(new DateOnly(2026, 7, 15));
    }
}

using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Tests.Application.Scheduling;

/// <summary>
/// "Today" for scheduling is the gym's (Africa/Cairo) calendar date. Deriving it from UTC
/// was wrong between local midnight and 02:00 (winter) / 03:00 (summer): the UTC date is
/// still yesterday, so yesterday's sessions counted as upcoming and yesterday could be booked.
/// </summary>
public class GymLocalTimeTests
{
    private static DateTime Utc(int y, int m, int d, int h, int min) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Theory]
    // Summer time (UTC+3)
    [InlineData(2026, 7, 14, 20, 59, 2026, 7, 14)] // 23:59 Cairo
    [InlineData(2026, 7, 14, 21, 0, 2026, 7, 15)]  // 00:00 Cairo — already tomorrow locally
    [InlineData(2026, 7, 14, 23, 30, 2026, 7, 15)] // 02:30 Cairo
    [InlineData(2026, 7, 15, 0, 0, 2026, 7, 15)]
    // Winter time (UTC+2)
    [InlineData(2026, 1, 14, 21, 59, 2026, 1, 14)] // 23:59 Cairo
    [InlineData(2026, 1, 14, 22, 0, 2026, 1, 15)]  // 00:00 Cairo
    [InlineData(2026, 1, 14, 12, 0, 2026, 1, 14)]
    public void Today_IsTheCairoCalendarDate(
        int y, int m, int d, int h, int min, int ey, int em, int ed)
    {
        GymLocalTime.Today(Utc(y, m, d, h, min)).Should().Be(new DateOnly(ey, em, ed));
    }

    [Fact]
    public void FromUtc_And_ToUtc_RoundTrip()
    {
        var utc = Utc(2026, 10, 12, 18, 0);

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

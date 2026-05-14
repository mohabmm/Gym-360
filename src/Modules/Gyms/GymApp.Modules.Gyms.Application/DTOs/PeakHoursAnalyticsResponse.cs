namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record PeakHoursAnalyticsResponse(
    Guid GymId,
    DateTime From,
    DateTime To,
    int TimezoneOffsetMinutes,
    int TotalCheckIns,
    IReadOnlyList<PeakHourBucket> Buckets);

public sealed record PeakHourBucket(
    int DayOfWeek,
    int Hour,
    int Count);

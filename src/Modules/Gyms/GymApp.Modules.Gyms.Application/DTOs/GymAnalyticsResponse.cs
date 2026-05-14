namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record GymAnalyticsResponse(
    Guid GymId,
    int TotalMembers,
    int TotalTrainers,
    int TotalStaff,
    int TotalClasses,
    int CheckInsToday,
    int CheckInsThisWeek,
    int CheckInsThisMonth,
    int ActiveBookings,
    IReadOnlyList<MemberGrowthPoint> MemberGrowth);

public sealed record MemberGrowthPoint(string Month, int Active, int New);

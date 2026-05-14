namespace GymApp.Modules.Scheduling.Application.Contracts;

public interface ISchedulingQueryService
{
    Task<int> GetCompletedSessionCountAsync(
        Guid trainerId,
        Guid gymId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<int> GetNoShowCountAsync(
        Guid memberId,
        Guid gymId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<UpcomingSessionInfo?> GetNextSessionAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task<int> GetTrainerSessionsForMonthAsync(
        Guid trainerId,
        Guid gymId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}

public sealed record UpcomingSessionInfo(
    Guid SessionId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledTime,
    int DurationMinutes,
    string SessionType);

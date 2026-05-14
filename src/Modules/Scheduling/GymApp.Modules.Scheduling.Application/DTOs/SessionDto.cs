namespace GymApp.Modules.Scheduling.Application.DTOs;

/// <summary>
/// Session summary shape returned to mobile and web clients.
/// <see cref="ScheduledAt"/> is the canonical client-facing timestamp; clients should treat it
/// as gym-local wall-clock combined with the gym's timezone (MVP assumes Africa/Cairo).
/// </summary>
public sealed record SessionDto(
    Guid Id,
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string SessionType,
    string? Focus,
    string Status,
    bool IsRecurring,
    DateTime CreatedAt);

namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record SessionDetailDto(
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
    string? RecurrenceRule,
    string? CancellationReason,
    DateTime? CancelledAt,
    DateTime CreatedAt,
    TrainerLogDto? TrainerLog,
    MemberLogDto? MemberLog,
    IReadOnlyList<SessionExerciseLogDto> ExerciseLogs);

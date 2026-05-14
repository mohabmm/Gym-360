namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record AvailabilityResponse(
    Guid TrainerId,
    Guid GymId,
    int SessionDurationMinutes,
    int BufferMinutes,
    List<AvailabilitySlotDto> WeeklySlots,
    List<BlockedDateDto> BlockedDates,
    DateTime UpdatedAt);

public sealed record BlockedDateDto(
    DateOnly Date,
    string? Reason,
    bool AutoNotifyClients);

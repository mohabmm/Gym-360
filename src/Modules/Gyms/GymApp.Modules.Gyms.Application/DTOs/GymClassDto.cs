namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record GymClassDto(
    Guid Id,
    Guid GymId,
    string Name,
    string? Description,
    Guid TrainerId,
    string? TrainerName,
    ClassScheduleDto Schedule,
    int Capacity,
    bool WaitlistEnabled,
    string Status,
    int BookedCount,
    DateTime CreatedAt);

namespace GymApp.Modules.Training.Application.DTOs;

public sealed record RemoteWorkoutStatusDto(
    bool IsActive,
    Guid? TrainerId,
    DateTime? ActivatedAt,
    DateTime? DeactivatedAt,
    string? Reason);

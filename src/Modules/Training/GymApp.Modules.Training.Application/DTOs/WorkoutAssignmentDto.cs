namespace GymApp.Modules.Training.Application.DTOs;

public sealed record WorkoutAssignmentDto(
    Guid Id,
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    Guid? WorkoutId,
    Guid? ProgramId,
    string AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status,
    string? Notes,
    bool IsRemoteMode);

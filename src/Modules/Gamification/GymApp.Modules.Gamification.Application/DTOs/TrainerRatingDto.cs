namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record TrainerRatingDto(
    Guid Id,
    Guid TrainerId,
    Guid ClientId,
    Guid SessionId,
    int Stars,
    string? Comment,
    DateTime CreatedAt);

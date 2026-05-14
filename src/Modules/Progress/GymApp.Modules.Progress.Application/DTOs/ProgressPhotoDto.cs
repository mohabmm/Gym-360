namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record ProgressPhotoDto(
    Guid Id,
    Guid UserId,
    Guid GymId,
    string PhotoUrl,
    string ThumbnailUrl,
    string Angle,
    DateOnly Date,
    bool IsSharedWithTrainer,
    string? Notes,
    DateTime CreatedAt);

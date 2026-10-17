using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.ValueObjects;

/// <summary>An uploaded progress photo: its image URLs, angle, day taken and sharing preference.</summary>
public sealed record ProgressPhotoDetails
{
    public required string PhotoUrl { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required PhotoAngle Angle { get; init; }
    public required DateOnly Date { get; init; }
    public bool IsSharedWithTrainer { get; init; }
    public string? Notes { get; init; }
}

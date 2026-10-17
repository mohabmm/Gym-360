using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>The editable content of an announcement and who it is aimed at.</summary>
public sealed record AnnouncementDetails
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? ImageUrl { get; init; }
    public required AnnouncementCategory Category { get; init; }
    public required TargetAudience TargetAudience { get; init; }
}

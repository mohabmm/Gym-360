namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record AnnouncementDto(
    Guid Id,
    Guid GymId,
    string Title,
    string Content,
    string? ImageUrl,
    string Category,
    string TargetAudience,
    DateTime? PublishedAt,
    DateTime? ScheduledAt,
    Guid CreatedBy);

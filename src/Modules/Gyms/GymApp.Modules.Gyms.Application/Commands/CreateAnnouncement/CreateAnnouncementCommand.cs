using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.CreateAnnouncement;

public sealed record CreateAnnouncementCommand(
    Guid GymId,
    string Title,
    string Content,
    string? ImageUrl,
    string Category,
    string TargetAudience,
    DateTime? ScheduledAt,
    Guid CreatedBy) : ICommand<Guid>;

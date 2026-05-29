using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateAnnouncement;

// All fields optional: the staff UI uses one PUT for both full edits and the
// publish/unpublish toggle (which sends only IsActive). The handler applies
// content changes only when Title is supplied, so a toggle never wipes fields.
public sealed record UpdateAnnouncementCommand(
    Guid GymId,
    Guid AnnouncementId,
    string? Title,
    string? Content,
    string? Category,
    string? TargetAudience,
    bool? IsActive) : ICommand;

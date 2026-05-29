using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.DeleteAnnouncement;

public sealed record DeleteAnnouncementCommand(Guid GymId, Guid AnnouncementId) : ICommand;

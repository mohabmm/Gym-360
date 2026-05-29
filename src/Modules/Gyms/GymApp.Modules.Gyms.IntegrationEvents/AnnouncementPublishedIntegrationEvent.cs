using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record AnnouncementPublishedIntegrationEvent(Guid GymId, Guid AnnouncementId, string Title) : IIntegrationEvent;

using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record AnnouncementPublishedDomainEvent(Guid GymId, Guid AnnouncementId) : IDomainEvent;

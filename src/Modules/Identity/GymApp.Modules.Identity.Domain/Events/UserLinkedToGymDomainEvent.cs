using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Events;

public sealed record UserLinkedToGymDomainEvent(Guid UserId, Guid GymId, string Role) : IDomainEvent;

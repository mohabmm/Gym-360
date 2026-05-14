using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;

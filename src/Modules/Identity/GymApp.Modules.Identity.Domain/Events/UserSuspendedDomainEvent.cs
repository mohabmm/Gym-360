using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Events;

public sealed record UserSuspendedDomainEvent(Guid UserId) : IDomainEvent;

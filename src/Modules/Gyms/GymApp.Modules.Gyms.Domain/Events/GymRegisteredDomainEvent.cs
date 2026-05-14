using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record GymRegisteredDomainEvent(Guid GymId) : IDomainEvent;

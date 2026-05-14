using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record GymReactivatedDomainEvent(Guid GymId) : IDomainEvent;

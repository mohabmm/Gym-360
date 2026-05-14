using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record SessionConfirmedDomainEvent(Guid SessionId) : IDomainEvent;

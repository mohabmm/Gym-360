using GymApp.Common.Domain;

namespace GymApp.Modules.Progress.Domain.DomainEvents;

public sealed record BodyMetricLoggedDomainEvent(Guid EntryId, Guid UserId) : IDomainEvent;

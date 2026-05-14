using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record ClassCancelledDomainEvent(Guid GymId, Guid ClassId) : IDomainEvent;

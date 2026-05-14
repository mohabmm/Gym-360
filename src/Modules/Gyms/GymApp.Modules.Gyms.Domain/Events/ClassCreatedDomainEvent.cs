using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record ClassCreatedDomainEvent(Guid GymId, Guid ClassId) : IDomainEvent;

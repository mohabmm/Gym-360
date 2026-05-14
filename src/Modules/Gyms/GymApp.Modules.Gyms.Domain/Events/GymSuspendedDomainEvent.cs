using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record GymSuspendedDomainEvent(Guid GymId, string Reason) : IDomainEvent;

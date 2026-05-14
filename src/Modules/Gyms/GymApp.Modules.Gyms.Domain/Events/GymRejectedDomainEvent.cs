using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record GymRejectedDomainEvent(Guid GymId, string Reason) : IDomainEvent;

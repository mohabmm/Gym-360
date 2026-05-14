using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record GymKioskPinClearedDomainEvent(Guid GymId) : IDomainEvent;

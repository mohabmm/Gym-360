using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record ClassBookedDomainEvent(Guid GymId, Guid ClassId, Guid MemberId) : IDomainEvent;

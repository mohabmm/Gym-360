using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record MemberFrozenDomainEvent(Guid GymId, Guid MemberId) : IDomainEvent;

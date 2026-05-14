using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record MemberUnfrozenDomainEvent(Guid GymId, Guid MemberId) : IDomainEvent;

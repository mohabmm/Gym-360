using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record MemberRemovedFromGymDomainEvent(Guid GymId, Guid MemberId) : IDomainEvent;

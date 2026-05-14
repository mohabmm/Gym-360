using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record MemberAddedToGymDomainEvent(Guid GymId, Guid MemberId) : IDomainEvent;

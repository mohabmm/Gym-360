using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record MembershipExpiredDomainEvent(
    Guid MemberId,
    Guid GymId) : IDomainEvent;

using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record MembershipExpiringDomainEvent(
    Guid MemberId,
    Guid GymId,
    DateTime ExpiresAt) : IDomainEvent;

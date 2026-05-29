using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

public sealed record MembershipExpiringIntegrationEvent(
    Guid MemberId,
    Guid GymId,
    DateTime ExpiresAt) : IIntegrationEvent;

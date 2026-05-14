using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

public sealed record PaymentReceivedIntegrationEvent(
    Guid PaymentId,
    Guid GymId,
    Guid MemberId,
    decimal Amount) : IDomainEvent;

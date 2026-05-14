using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record PaymentReceivedDomainEvent(
    Guid PaymentId,
    Guid GymId,
    decimal Amount) : IDomainEvent;

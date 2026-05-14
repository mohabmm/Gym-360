using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record PaymentVoidedDomainEvent(
    Guid PaymentId,
    Guid GymId,
    string Reason) : IDomainEvent;

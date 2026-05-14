using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record GymPlanChangedDomainEvent(
    Guid GymId,
    Guid OldPlanId,
    Guid NewPlanId) : IDomainEvent;

using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record SessionPackageLowDomainEvent(
    Guid PackageId,
    Guid MemberId,
    int Remaining) : IDomainEvent;

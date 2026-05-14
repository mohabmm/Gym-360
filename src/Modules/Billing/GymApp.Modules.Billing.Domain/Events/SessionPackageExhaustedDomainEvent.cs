using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

public sealed record SessionPackageExhaustedDomainEvent(
    Guid PackageId,
    Guid MemberId) : IDomainEvent;

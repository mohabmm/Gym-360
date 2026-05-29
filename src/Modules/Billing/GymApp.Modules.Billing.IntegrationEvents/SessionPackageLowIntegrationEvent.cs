using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

public sealed record SessionPackageLowIntegrationEvent(
    Guid PackageId,
    Guid MemberId,
    Guid GymId,
    int Remaining) : IIntegrationEvent;

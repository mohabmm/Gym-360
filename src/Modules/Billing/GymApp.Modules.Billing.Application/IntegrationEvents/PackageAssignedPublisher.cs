using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.IntegrationEvents;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

/// <summary>
/// Announces a package the desk just created, so Communication can tell the coach about the
/// client. Call before SaveChanges, so the event commits with the package.
/// </summary>
internal static class PackageAssignedPublisher
{
    public static void Enqueue(IIntegrationEventPublisher publisher, SessionPackage package) =>
        publisher.Enqueue(new SessionPackageAssignedIntegrationEvent(
            package.Id,
            package.GymId,
            package.MemberId,
            package.TrainerId,
            package.IsTrial,
            package.Name,
            package.TotalSessions));
}

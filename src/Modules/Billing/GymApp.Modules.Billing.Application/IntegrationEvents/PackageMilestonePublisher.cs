using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.IntegrationEvents;
using Milestones = GymApp.Modules.Billing.IntegrationEvents.SessionPackageMilestoneReachedIntegrationEvent.Milestones;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

/// <summary>
/// Publishes the package's newly reached milestones (renewal 3/1 left, trial last session,
/// trial finished) so Communication can notify the trainee, coach and desk. Call after a
/// session is counted and before SaveChanges, so the events commit with the deduction.
/// </summary>
internal static class PackageMilestonePublisher
{
    public static void Enqueue(IIntegrationEventPublisher publisher, SessionPackage package)
    {
        var reached = package.GetDomainEvents().OfType<SessionPackageMilestoneReachedDomainEvent>();

        foreach (var e in reached)
        {
            publisher.Enqueue(new SessionPackageMilestoneReachedIntegrationEvent(
                e.PackageId,
                e.GymId,
                e.MemberId,
                e.TrainerId,
                ToName(e.Milestone),
                e.Remaining,
                package.TotalSessions,
                package.IsTrial));
        }
    }

    private static string ToName(PackageMilestone milestone) => milestone switch
    {
        PackageMilestone.RenewalThreeLeft => Milestones.RenewalThreeLeft,
        PackageMilestone.RenewalOneLeft => Milestones.RenewalOneLeft,
        PackageMilestone.TrialLastSession => Milestones.TrialLastSession,
        _ => Milestones.TrialFinished,
    };
}

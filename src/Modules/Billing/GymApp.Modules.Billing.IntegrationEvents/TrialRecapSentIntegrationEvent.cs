using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

/// <summary>pt.trial_recap: the coach sent the trainee their post-trial plan.</summary>
public sealed record TrialRecapSentIntegrationEvent(
    Guid PackageId,
    Guid GymId,
    Guid MemberId,
    Guid TrainerId,
    int RecommendedSessions) : IIntegrationEvent;

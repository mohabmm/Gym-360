using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

/// <summary>
/// pt.package_interest: a trainee tapped "I'm interested". <see cref="Source"/> is
/// <c>TrialRecap</c>, <c>TrialFollowUp</c> or <c>Renewal</c>.
/// </summary>
public sealed record PackageInterestCreatedIntegrationEvent(
    Guid InterestId,
    Guid GymId,
    Guid MemberId,
    Guid TrainerId,
    string Source,
    int? RecommendedSessions) : IIntegrationEvent;

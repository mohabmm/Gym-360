using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.IntegrationEvents;

/// <summary>pt.dual_session_logging: the trainee said a logged session did not happen; the desk reviews it.</summary>
public sealed record SessionDisputedIntegrationEvent(
    Guid SessionId,
    Guid TrainerId,
    Guid MemberId,
    Guid GymId) : IIntegrationEvent;

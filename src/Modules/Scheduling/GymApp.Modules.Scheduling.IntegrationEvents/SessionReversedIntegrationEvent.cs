using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.IntegrationEvents;

/// <summary>pt.dual_session_logging: the desk reversed a disputed session; Billing gives the package session back.</summary>
public sealed record SessionReversedIntegrationEvent(
    Guid SessionId,
    Guid TrainerId,
    Guid MemberId,
    Guid GymId) : IIntegrationEvent;

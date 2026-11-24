using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

/// <summary>
/// The desk gave <paramref name="MemberId"/> a session package with <paramref name="TrainerId"/>
/// (trial, free or paid; renewals included). pt.starter_plan tells the coach about the new client.
/// </summary>
public sealed record SessionPackageAssignedIntegrationEvent(
    Guid PackageId,
    Guid GymId,
    Guid MemberId,
    Guid TrainerId,
    bool IsTrial,
    string PackageName,
    int TotalSessions) : IIntegrationEvent;

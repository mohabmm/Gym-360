namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record DeployResult(
    string Scope,
    Guid? TargetId,
    int AffectedGyms,
    DateTime DeployedAt);

namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record PlanFeatureToggleDto(
    Guid FeatureId,
    bool IsEnabled,
    int? LimitValue);

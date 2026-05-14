namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record PlanFeaturesResponse(
    Guid GymId,
    string PlanCode,
    string PlanName,
    int MemberLimit,
    int TrainerLimit,
    int StaffLimit,
    List<PlanFeatureItemDto> Features);

public sealed record PlanFeatureItemDto(
    string FeatureCode,
    string FeatureName,
    bool IsEnabled,
    int? LimitValue,
    bool IsOverridden);

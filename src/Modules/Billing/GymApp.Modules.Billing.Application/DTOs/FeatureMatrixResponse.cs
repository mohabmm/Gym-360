namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record FeatureMatrixResponse(
    List<SubscriptionPlanDto> Plans,
    List<FeatureDto> Features,
    List<PlanFeatureEntryDto> Matrix);

public sealed record FeatureDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? Category,
    bool IsActive,
    int SortOrder);

public sealed record PlanFeatureEntryDto(
    Guid PlanId,
    Guid FeatureId,
    bool IsEnabled,
    int? LimitValue);

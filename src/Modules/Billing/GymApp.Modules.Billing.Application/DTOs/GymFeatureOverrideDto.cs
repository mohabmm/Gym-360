namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record GymFeatureOverrideDto(
    Guid Id,
    Guid GymId,
    Guid FeatureId,
    string FeatureCode,
    string FeatureName,
    bool OverrideValue,
    string? Reason,
    Guid SetBy,
    DateTime SetAt);

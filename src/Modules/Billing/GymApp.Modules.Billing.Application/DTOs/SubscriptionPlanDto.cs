namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record SubscriptionPlanDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal MonthlyPriceEgp,
    decimal AnnualPriceEgp,
    int MemberLimit,
    int TrainerLimit,
    int StaffLimit,
    bool IsActive,
    bool IsPublic,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

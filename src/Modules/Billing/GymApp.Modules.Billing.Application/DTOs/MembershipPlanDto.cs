namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record MembershipPlanDto(
    Guid Id,
    Guid GymId,
    string Name,
    int DurationDays,
    decimal Price,
    string Currency,
    List<string> Features,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAt);

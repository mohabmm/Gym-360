namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record PackageTemplateDto(
    Guid Id,
    Guid GymId,
    string Name,
    int SessionCount,
    decimal PricePerSession,
    decimal TotalPrice,
    string Currency,
    bool IsActive,
    DateTime CreatedAt);

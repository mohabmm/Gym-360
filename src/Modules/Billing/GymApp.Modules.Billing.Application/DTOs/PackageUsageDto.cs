namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record PackageUsageDto(
    Guid Id,
    Guid SessionId,
    DateTime UsedAt,
    string UsageType);

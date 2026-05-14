namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record SessionPackageDto(
    Guid Id,
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    Guid TemplateId,
    string Name,
    int TotalSessions,
    int RemainingSessions,
    decimal PricePerSession,
    decimal TotalPrice,
    string Currency,
    string Status,
    DateTime? ExpiresAt,
    List<PackageUsageDto> UsageHistory,
    Guid AssignedBy,
    DateTime CreatedAt);

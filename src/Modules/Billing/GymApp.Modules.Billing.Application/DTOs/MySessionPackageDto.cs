namespace GymApp.Modules.Billing.Application.DTOs;

/// <summary>
/// Trainee-facing projection of a SessionPackage as defined by the
/// `/api/v1/billing/me/packages` PRD. Distinct from the internal
/// SessionPackageDto to avoid leaking internals (e.g. AssignedBy, UsageHistory)
/// and to match the nested-money response shape.
/// </summary>
public sealed record MySessionPackageDto(
    Guid Id,
    string Name,
    Guid TrainerId,
    string? TrainerName,
    int TotalSessions,
    int RemainingSessions,
    string Status,
    MoneyDto PricePerSession,
    MoneyDto TotalPrice,
    DateTime AssignedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);

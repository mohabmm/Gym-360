using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record DataImportSessionPackageIntegrationEvent(
    Guid ImportJobId,
    int RowNumber,
    Guid GymId,
    string MemberNumber,
    string? TrainerMemberNumber,
    string PackageName,
    int TotalSessions,
    int RemainingSessions,
    decimal TotalPrice,
    string Currency,
    DateTime PurchasedAt,
    DateTime? ExpiresAt) : IIntegrationEvent;

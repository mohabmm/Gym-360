namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record DataImportJobDto(
    Guid Id,
    Guid GymId,
    Guid UploadedByUserId,
    string Source,
    string Status,
    string OriginalFileName,
    long FileSizeBytes,
    int TotalRowCount,
    int SuccessCount,
    int FailureCount,
    bool HasErrorReport,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

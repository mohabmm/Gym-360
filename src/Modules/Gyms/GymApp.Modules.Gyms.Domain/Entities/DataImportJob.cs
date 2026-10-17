using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class DataImportJob : Entity
{
    private DataImportJob() { }

    public Guid GymId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DataImportSource Source { get; private set; }
    public DataImportStatus Status { get; private set; }
    public string OriginalFileName { get; private set; } = default!;
    public string StoragePath { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public int TotalRowCount { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public string? ErrorReportPath { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static DataImportJob Create(Guid gymId, Guid uploadedByUserId, DataImportFile file)
    {
        var job = new DataImportJob
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            UploadedByUserId = uploadedByUserId,
            Source = file.Source,
            Status = DataImportStatus.Pending,
            OriginalFileName = file.OriginalFileName,
            StoragePath = file.StoragePath,
            FileSizeBytes = file.FileSizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        job.RaiseDomainEvent(new DataImportJobCreatedDomainEvent(job.Id, gymId));
        return job;
    }

    public Result MarkProcessing()
    {
        if (Status != DataImportStatus.Pending)
            return Result.Failure(GymErrors.DataImportInvalidTransition);

        Status = DataImportStatus.Processing;
        StartedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkCompleted(int totalRows, int successCount, int failureCount, string? errorReportPath)
    {
        if (Status != DataImportStatus.Processing)
            return Result.Failure(GymErrors.DataImportInvalidTransition);

        TotalRowCount = totalRows;
        SuccessCount = successCount;
        FailureCount = failureCount;
        ErrorReportPath = errorReportPath;
        Status = failureCount == 0 ? DataImportStatus.Completed : DataImportStatus.CompletedWithErrors;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DataImportJobCompletedDomainEvent(Id, GymId, successCount, failureCount));
        return Result.Success();
    }

    public Result MarkFailed(string reason)
    {
        if (Status is DataImportStatus.Completed or DataImportStatus.CompletedWithErrors or DataImportStatus.Failed)
            return Result.Failure(GymErrors.DataImportInvalidTransition);

        Status = DataImportStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DataImportJobFailedDomainEvent(Id, GymId, reason));
        return Result.Success();
    }
}

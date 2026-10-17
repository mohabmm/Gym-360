using FluentAssertions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Gyms;

public sealed class DataImportJobTests
{
    private static DataImportJob CreatePending() =>
        DataImportJob.Create(
            gymId: Guid.NewGuid(),
            uploadedByUserId: Guid.NewGuid(),
            file: new DataImportFile
            {
                Source = DataImportSource.Excel,
                OriginalFileName = "roster.xlsx",
                StoragePath = "/tmp/roster.xlsx",
                FileSizeBytes = 2048
            });

    [Fact]
    public void Create_raises_created_event_and_sets_pending_status()
    {
        var job = CreatePending();

        job.Status.Should().Be(DataImportStatus.Pending);
        job.SuccessCount.Should().Be(0);
        job.FailureCount.Should().Be(0);
        job.GetDomainEvents().Should().ContainSingle(e => e is DataImportJobCreatedDomainEvent);
    }

    [Fact]
    public void MarkProcessing_transitions_from_pending()
    {
        var job = CreatePending();

        var result = job.MarkProcessing();

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(DataImportStatus.Processing);
        job.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkProcessing_fails_if_not_pending()
    {
        var job = CreatePending();
        job.MarkProcessing();

        var second = job.MarkProcessing();

        second.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkCompleted_with_no_errors_yields_completed_status()
    {
        var job = CreatePending();
        job.MarkProcessing();

        var result = job.MarkCompleted(totalRows: 10, successCount: 10, failureCount: 0, errorReportPath: null);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(DataImportStatus.Completed);
        job.SuccessCount.Should().Be(10);
        job.CompletedAt.Should().NotBeNull();
        job.GetDomainEvents().Should().Contain(e => e is DataImportJobCompletedDomainEvent);
    }

    [Fact]
    public void MarkCompleted_with_failures_yields_completed_with_errors_status()
    {
        var job = CreatePending();
        job.MarkProcessing();

        job.MarkCompleted(totalRows: 10, successCount: 7, failureCount: 3, errorReportPath: "errors/x.csv");

        job.Status.Should().Be(DataImportStatus.CompletedWithErrors);
        job.FailureCount.Should().Be(3);
        job.ErrorReportPath.Should().Be("errors/x.csv");
    }

    [Fact]
    public void MarkFailed_sets_status_and_reason()
    {
        var job = CreatePending();
        job.MarkProcessing();

        job.MarkFailed("parser crashed");

        job.Status.Should().Be(DataImportStatus.Failed);
        job.FailureReason.Should().Be("parser crashed");
        job.GetDomainEvents().Should().Contain(e => e is DataImportJobFailedDomainEvent);
    }

    [Fact]
    public void MarkFailed_rejected_after_completion()
    {
        var job = CreatePending();
        job.MarkProcessing();
        job.MarkCompleted(1, 1, 0, null);

        var result = job.MarkFailed("too late");

        result.IsFailure.Should().BeTrue();
    }
}

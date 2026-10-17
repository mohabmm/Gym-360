using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Quartz;

namespace GymApp.Modules.Gyms.Infrastructure.Jobs;

[DisallowConcurrentExecution]
internal sealed class ProcessDataImportJob : IJob
{
    public const string JobDataKey = "ImportJobId";

    private readonly IDataImportJobRepository _jobRepository;
    private readonly DataImportFileProcessor _fileProcessor;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessDataImportJob> _logger;

    public ProcessDataImportJob(
        IDataImportJobRepository jobRepository,
        DataImportFileProcessor fileProcessor,
        IIntegrationEventPublisher integrationEvents,
        IGymsUnitOfWork unitOfWork,
        ILogger<ProcessDataImportJob> logger)
    {
        _jobRepository = jobRepository;
        _fileProcessor = fileProcessor;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var jobId = Guid.Parse(context.MergedJobDataMap.GetString(JobDataKey)!);

        var job = await StartProcessingAsync(jobId, ct);
        if (job is null)
            return;

        try
        {
            await ImportAsync(job, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataImport job {JobId} failed.", jobId);
            job.MarkFailed(ex.Message.Length > 900 ? ex.Message[..900] : ex.Message);
            await SaveAsync(job, ct);
        }
    }

    private async Task<DataImportJob?> StartProcessingAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogWarning("DataImport job {JobId} not found — skipping.", jobId);
            return null;
        }

        var processingResult = job.MarkProcessing();
        if (processingResult.IsFailure)
        {
            _logger.LogWarning("DataImport job {JobId} cannot transition to Processing: {Error}",
                jobId, processingResult.Error);
            return null;
        }

        await SaveAsync(job, ct);
        return job;
    }

    private async Task ImportAsync(DataImportJob job, CancellationToken ct)
    {
        var parsed = await _fileProcessor.ParseAsync(job, ct);

        // Enqueue onto the outbox; the rows are flushed by the job-completion SaveChanges
        // below (same intercepted GymsDbContext, same g{N}_outbox) and delivered durably.
        foreach (var e in parsed.Members) _integrationEvents.Enqueue(e);
        foreach (var e in parsed.Payments) _integrationEvents.Enqueue(e);
        foreach (var e in parsed.Packages) _integrationEvents.Enqueue(e);
        foreach (var e in parsed.InBodyRecords) _integrationEvents.Enqueue(e);

        var errorReportPath = await _fileProcessor.SaveErrorReportAsync(job.Id, parsed.Errors, ct);

        var totalRows = parsed.Members.Count + parsed.Payments.Count
                      + parsed.Packages.Count + parsed.InBodyRecords.Count
                      + parsed.Errors.Count;

        job.MarkCompleted(
            totalRows,
            successCount: totalRows - parsed.Errors.Count,
            failureCount: parsed.Errors.Count,
            errorReportPath);

        await SaveAsync(job, ct);

        _logger.LogInformation(
            "DataImport job {JobId} completed: {Success} succeeded, {Fail} failed.",
            job.Id, totalRows - parsed.Errors.Count, parsed.Errors.Count);
    }

    private async Task SaveAsync(DataImportJob job, CancellationToken ct)
    {
        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

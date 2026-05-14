using System.Text;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;

namespace GymApp.Modules.Gyms.Infrastructure.Jobs;

[DisallowConcurrentExecution]
internal sealed class ProcessDataImportJob : IJob
{
    public const string JobDataKey = "ImportJobId";

    private readonly IDataImportJobRepository _jobRepository;
    private readonly IImportFileStorage _fileStorage;
    private readonly IEnumerable<IImportParser> _parsers;
    private readonly IPublisher _publisher;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessDataImportJob> _logger;

    public ProcessDataImportJob(
        IDataImportJobRepository jobRepository,
        IImportFileStorage fileStorage,
        IEnumerable<IImportParser> parsers,
        IPublisher publisher,
        IGymsUnitOfWork unitOfWork,
        ILogger<ProcessDataImportJob> logger)
    {
        _jobRepository = jobRepository;
        _fileStorage = fileStorage;
        _parsers = parsers;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var jobId = Guid.Parse(context.MergedJobDataMap.GetString(JobDataKey)!);

        var job = await _jobRepository.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogWarning("DataImport job {JobId} not found — skipping.", jobId);
            return;
        }

        var processingResult = job.MarkProcessing();
        if (processingResult.IsFailure)
        {
            _logger.LogWarning("DataImport job {JobId} cannot transition to Processing: {Error}",
                jobId, processingResult.Error);
            return;
        }

        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            var parser = _parsers.FirstOrDefault(p => p.CanHandle(job.Source))
                ?? throw new InvalidOperationException($"No parser registered for {job.Source}");

            await using var content = await _fileStorage.OpenAsync(job.StoragePath, ct);
            var parsed = await parser.ParseAsync(job.Id, job.GymId, content, ct);

            foreach (var e in parsed.Members) await _publisher.Publish(e, ct);
            foreach (var e in parsed.Payments) await _publisher.Publish(e, ct);
            foreach (var e in parsed.Packages) await _publisher.Publish(e, ct);
            foreach (var e in parsed.InBodyRecords) await _publisher.Publish(e, ct);

            string? errorReportPath = null;
            if (parsed.Errors.Count > 0)
            {
                errorReportPath = await SaveErrorReportAsync(job.Id, parsed.Errors, ct);
            }

            var totalRows = parsed.Members.Count + parsed.Payments.Count
                          + parsed.Packages.Count + parsed.InBodyRecords.Count
                          + parsed.Errors.Count;

            job.MarkCompleted(
                totalRows,
                successCount: totalRows - parsed.Errors.Count,
                failureCount: parsed.Errors.Count,
                errorReportPath);

            _jobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DataImport job {JobId} completed: {Success} succeeded, {Fail} failed.",
                jobId, totalRows - parsed.Errors.Count, parsed.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataImport job {JobId} failed.", jobId);
            job.MarkFailed(ex.Message.Length > 900 ? ex.Message[..900] : ex.Message);
            _jobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private async Task<string> SaveErrorReportAsync(Guid jobId, IReadOnlyList<ImportRowError> errors, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sheet,RowNumber,Column,Message");
        foreach (var e in errors)
        {
            sb.Append(Csv(e.Sheet)).Append(',')
              .Append(e.RowNumber).Append(',')
              .Append(Csv(e.Column)).Append(',')
              .AppendLine(Csv(e.Message));
        }

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        return await _fileStorage.SaveErrorReportAsync(jobId, ms, ct);
    }

    private static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}

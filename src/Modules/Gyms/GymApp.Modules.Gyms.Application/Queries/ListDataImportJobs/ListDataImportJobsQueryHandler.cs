using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.ListDataImportJobs;

internal sealed class ListDataImportJobsQueryHandler : IQueryHandler<ListDataImportJobsQuery, IReadOnlyList<DataImportJobDto>>
{
    private readonly IDataImportJobRepository _repository;

    public ListDataImportJobsQueryHandler(IDataImportJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<DataImportJobDto>>> Handle(ListDataImportJobsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take == 0 ? 20 : request.Take, 1, 100);
        var skip = Math.Max(0, request.Skip);

        var jobs = await _repository.ListByGymAsync(request.GymId, skip, take, cancellationToken);

        var dtos = jobs.Select(job => new DataImportJobDto(
            job.Id,
            job.GymId,
            job.UploadedByUserId,
            job.Source.ToString(),
            job.Status.ToString(),
            job.OriginalFileName,
            job.FileSizeBytes,
            job.TotalRowCount,
            job.SuccessCount,
            job.FailureCount,
            !string.IsNullOrEmpty(job.ErrorReportPath),
            job.FailureReason,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt)).ToList();

        return Result.Success<IReadOnlyList<DataImportJobDto>>(dtos);
    }
}

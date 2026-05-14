using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetDataImportJob;

internal sealed class GetDataImportJobQueryHandler : IQueryHandler<GetDataImportJobQuery, DataImportJobDto>
{
    private readonly IDataImportJobRepository _repository;

    public GetDataImportJobQueryHandler(IDataImportJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DataImportJobDto>> Handle(GetDataImportJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken);
        if (job is null || job.GymId != request.GymId)
            return Result.Failure<DataImportJobDto>(GymErrors.DataImportJobNotFound);

        return Result.Success(new DataImportJobDto(
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
            job.CompletedAt));
    }
}

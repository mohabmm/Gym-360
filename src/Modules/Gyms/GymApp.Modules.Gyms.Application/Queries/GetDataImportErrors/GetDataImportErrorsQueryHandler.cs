using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetDataImportErrors;

internal sealed class GetDataImportErrorsQueryHandler : IQueryHandler<GetDataImportErrorsQuery, Stream>
{
    private readonly IDataImportJobRepository _repository;
    private readonly IImportFileStorage _fileStorage;

    public GetDataImportErrorsQueryHandler(
        IDataImportJobRepository repository,
        IImportFileStorage fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<Stream>> Handle(GetDataImportErrorsQuery request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken);
        if (job is null || job.GymId != request.GymId || string.IsNullOrEmpty(job.ErrorReportPath))
            return Result.Failure<Stream>(GymErrors.DataImportJobNotFound);

        var stream = await _fileStorage.OpenErrorReportAsync(job.ErrorReportPath, cancellationToken);
        return Result.Success(stream);
    }
}

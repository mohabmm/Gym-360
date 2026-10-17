using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.StartDataImport;

/// <summary>
/// Stores the uploaded file, persists a pending <see cref="DataImportJob"/> for it and
/// hands the job to the background scheduler.
/// </summary>
internal sealed class DataImportJobLauncher
{
    private readonly IDataImportJobRepository _jobRepository;
    private readonly IImportFileStorage _fileStorage;
    private readonly IImportJobScheduler _scheduler;
    private readonly IGymsUnitOfWork _unitOfWork;

    public DataImportJobLauncher(
        IDataImportJobRepository jobRepository,
        IImportFileStorage fileStorage,
        IImportJobScheduler scheduler,
        IGymsUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _fileStorage = fileStorage;
        _scheduler = scheduler;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> LaunchAsync(StartDataImportCommand request, CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();
        var storagePath = await _fileStorage.SaveUploadAsync(
            new ImportUpload(request.GymId, jobId, request.OriginalFileName, request.Content), cancellationToken);

        var job = DataImportJob.Create(
            request.GymId,
            request.UploadedByUserId,
            new DataImportFile
            {
                Source = request.Source,
                OriginalFileName = request.OriginalFileName,
                StoragePath = storagePath,
                FileSizeBytes = request.FileSizeBytes
            });

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, jobId);

        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _scheduler.EnqueueAsync(job.Id, cancellationToken);

        return job.Id;
    }
}

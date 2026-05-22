using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.StartDataImport;

internal sealed class StartDataImportCommandHandler : ICommandHandler<StartDataImportCommand, Guid>
{
    public const string FeatureCode = "data_import";

    private readonly IGymRepository _gymRepository;
    private readonly IDataImportJobRepository _jobRepository;
    private readonly IImportFileStorage _fileStorage;
    private readonly IGymPlanService _gymPlanService;
    private readonly IImportJobScheduler _scheduler;
    private readonly IGymsUnitOfWork _unitOfWork;

    public StartDataImportCommandHandler(
        IGymRepository gymRepository,
        IDataImportJobRepository jobRepository,
        IImportFileStorage fileStorage,
        IGymPlanService gymPlanService,
        IImportJobScheduler scheduler,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _jobRepository = jobRepository;
        _fileStorage = fileStorage;
        _gymPlanService = gymPlanService;
        _scheduler = scheduler;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(StartDataImportCommand request, CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(request.GymId, FeatureCode, cancellationToken);
        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(FeatureCode, currentPlan, "Pro"));
        }

        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<Guid>(GymErrors.NotFound);

        var jobId = Guid.NewGuid();
        var storagePath = await _fileStorage.SaveUploadAsync(
            request.GymId, jobId, request.OriginalFileName, request.Content, cancellationToken);

        var job = DataImportJob.Create(
            request.GymId,
            request.UploadedByUserId,
            request.Source,
            request.OriginalFileName,
            storagePath,
            request.FileSizeBytes);

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, jobId);

        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _scheduler.EnqueueAsync(job.Id, cancellationToken);

        return Result.Success(job.Id);
    }
}

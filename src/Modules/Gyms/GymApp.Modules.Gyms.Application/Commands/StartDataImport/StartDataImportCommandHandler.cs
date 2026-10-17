using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.StartDataImport;

internal sealed class StartDataImportCommandHandler : ICommandHandler<StartDataImportCommand, Guid>
{
    public const string FeatureCode = "data_import";

    private readonly IGymPlanService _gymPlanService;
    private readonly IGymRepository _gymRepository;
    private readonly DataImportJobLauncher _launcher;

    public StartDataImportCommandHandler(
        IGymPlanService gymPlanService,
        IGymRepository gymRepository,
        DataImportJobLauncher launcher)
    {
        _gymPlanService = gymPlanService;
        _gymRepository = gymRepository;
        _launcher = launcher;
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

        var jobId = await _launcher.LaunchAsync(request, cancellationToken);

        return Result.Success(jobId);
    }
}

using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Commands.LogWater;

internal sealed class LogWaterCommandHandler : ICommandHandler<LogWaterCommand, Guid>
{
    private readonly IWaterLogRepository _waterLogRepository;
    private readonly IGymPlanService _gymPlanService;

    public LogWaterCommandHandler(
        IWaterLogRepository waterLogRepository,
        IGymPlanService gymPlanService)
    {
        _waterLogRepository = waterLogRepository;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result<Guid>> Handle(LogWaterCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "nutrition_tracking", cancellationToken);

        if (!isEnabled)
        {
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                "nutrition_tracking",
                await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken),
                "Starter"));
        }

        // The repository handles the unique-index race on (UserId, Date) — see
        // WaterLogRepository.UpsertAndAppendEntryAsync.
        var waterLog = await _waterLogRepository.UpsertAndAppendEntryAsync(
            request.UserId,
            request.GymId,
            request.Date,
            request.AmountMl,
            cancellationToken);

        return Result.Success(waterLog.Id);
    }
}

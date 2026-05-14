using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Application.Abstractions;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetric;

internal sealed class LogBodyMetricCommandHandler : ICommandHandler<LogBodyMetricCommand, Guid>
{
    private readonly IBodyMetricEntryRepository _repository;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly IGymPlanService _planService;

    public LogBodyMetricCommandHandler(
        IBodyMetricEntryRepository repository,
        IProgressUnitOfWork unitOfWork,
        IGymPlanService planService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _planService = planService;
    }

    public async Task<Result<Guid>> Handle(LogBodyMetricCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _planService.IsFeatureEnabledAsync(
            request.GymId, "progress_tracking", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _planService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(
                PlanErrors.FeatureNotAvailable("progress_tracking", currentPlan, "Pro"));
        }

        if (!Enum.TryParse<WeightUnit>(request.WeightUnit, true, out var weightUnit))
            weightUnit = WeightUnit.Kg;

        if (!Enum.TryParse<MetricSource>(request.Source, true, out var source))
            source = MetricSource.SelfReported;

        // Unique index (user_id, date) — surface as CONFLICT instead of a DB crash.
        var existing = await _repository.GetByUserAndDateAsync(
            request.UserId, request.GymId, request.Date, cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(ProgressErrors.DuplicateMetricEntry);
        }

        var measurements = ProgressMapper.ToDomain(request.Measurements);

        var result = BodyMetricEntry.Create(
            request.UserId,
            request.GymId,
            request.Date,
            request.Weight,
            weightUnit,
            request.BodyFatPercentage,
            measurements,
            source,
            request.LoggedBy,
            request.Notes);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}

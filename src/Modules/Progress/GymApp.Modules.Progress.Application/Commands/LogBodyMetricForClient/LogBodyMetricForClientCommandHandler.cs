using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetricForClient;

internal sealed class LogBodyMetricForClientCommandHandler : ICommandHandler<LogBodyMetricForClientCommand, Guid>
{
    private readonly IBodyMetricEntryRepository _repository;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly IGymPlanService _planService;

    public LogBodyMetricForClientCommandHandler(
        IBodyMetricEntryRepository repository,
        IProgressUnitOfWork unitOfWork,
        IGymPlanService planService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _planService = planService;
    }

    public async Task<Result<Guid>> Handle(LogBodyMetricForClientCommand request, CancellationToken cancellationToken)
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

        // The entry table has a unique index on (user_id, date). Checking here lets us
        // surface a clean CONFLICT to the client instead of letting SaveChanges throw
        // a DbUpdateException that maps to 500.
        var existing = await _repository.GetByUserAndDateAsync(
            request.ClientId, request.GymId, request.Date, cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(ProgressErrors.DuplicateMetricEntry);
        }

        var measurements = ProgressMapper.ToDomain(request.Measurements);

        var result = BodyMetricEntry.Create(new BodyMetricEntryDetails
        {
            UserId = request.ClientId,
            GymId = request.GymId,
            Date = request.Date,
            Source = MetricSource.TrainerMeasured,
            LoggedBy = request.TrainerId,
            Reading = new BodyMetricReading
            {
                Weight = request.Weight,
                WeightUnit = weightUnit,
                BodyFatPercentage = request.BodyFatPercentage,
                Measurements = measurements,
                Notes = request.Notes
            }
        });

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}

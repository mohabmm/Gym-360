using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Commands.LogInBodyScan;

/// <summary>
/// Saves an InBody scan for the member. One entry per day: a scan on a day that already has an
/// entry updates it (InBody values win, tape measurements are kept) instead of failing.
/// </summary>
internal sealed class LogInBodyScanCommandHandler : ICommandHandler<LogInBodyScanCommand, LogInBodyScanResult>
{
    internal const string FeatureCode = "inbody_integration";

    private readonly IBodyMetricEntryRepository _repository;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly IGymPlanService _planService;

    public LogInBodyScanCommandHandler(
        IBodyMetricEntryRepository repository,
        IProgressUnitOfWork unitOfWork,
        IGymPlanService planService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _planService = planService;
    }

    public async Task<Result<LogInBodyScanResult>> Handle(LogInBodyScanCommand request, CancellationToken cancellationToken)
    {
        if (!await _planService.IsFeatureEnabledAsync(request.GymId, FeatureCode, cancellationToken))
        {
            var currentPlan = await _planService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<LogInBodyScanResult>(
                PlanErrors.FeatureNotAvailable(FeatureCode, currentPlan, "Business"));
        }

        var reading = ToReading(request);
        var existing = await _repository.GetByUserAndDateAsync(
            request.UserId, request.GymId, request.Date, cancellationToken);

        var result = existing is null
            ? await AddAsync(request, reading, cancellationToken)
            : Merge(existing, reading, request.UserId);

        if (result.IsSuccess)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private async Task<Result<LogInBodyScanResult>> AddAsync(
        LogInBodyScanCommand request, BodyMetricReading reading, CancellationToken cancellationToken)
    {
        var created = BodyMetricEntry.Create(new BodyMetricEntryDetails
        {
            UserId = request.UserId,
            GymId = request.GymId,
            Date = request.Date,
            Source = MetricSource.InBodyScan,
            LoggedBy = request.UserId,
            Reading = reading
        });

        if (created.IsFailure)
        {
            return Result.Failure<LogInBodyScanResult>(created.Error);
        }

        await _repository.AddAsync(created.Value, cancellationToken);
        return Result.Success(new LogInBodyScanResult(created.Value.Id, Created: true));
    }

    private Result<LogInBodyScanResult> Merge(BodyMetricEntry existing, BodyMetricReading reading, Guid loggedBy)
    {
        var merged = existing.RecordInBodyScan(reading, loggedBy);
        if (merged.IsFailure)
        {
            return Result.Failure<LogInBodyScanResult>(merged.Error);
        }

        _repository.Update(existing);
        return Result.Success(new LogInBodyScanResult(existing.Id, Created: false));
    }

    private static BodyMetricReading ToReading(LogInBodyScanCommand request)
    {
        var values = request.Values;
        return new BodyMetricReading
        {
            Weight = values.WeightKg,
            WeightUnit = WeightUnit.Kg,
            BodyFatPercentage = values.BodyFatPercentage,
            Measurements = null,
            Composition = new BodyComposition
            {
                HeightCm = values.HeightCm,
                SkeletalMuscleMassKg = values.SkeletalMuscleMassKg,
                BodyWaterKg = values.BodyWaterKg
            },
            Notes = request.Notes
        };
    }
}

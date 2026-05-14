using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Modules.Gyms.Application.Queries.GetPeakHoursAnalytics;

internal sealed class GetPeakHoursAnalyticsQueryHandler
    : IQueryHandler<GetPeakHoursAnalyticsQuery, PeakHoursAnalyticsResponse>
{
    public const string FeatureCode = "peak_hours_analytics";

    private const int DefaultRangeDays = 30;

    private readonly IGymQueryService _gymQueryService;
    private readonly IGymPlanService _gymPlanService;

    public GetPeakHoursAnalyticsQueryHandler(
        IGymQueryService gymQueryService,
        IGymPlanService gymPlanService)
    {
        _gymQueryService = gymQueryService;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result<PeakHoursAnalyticsResponse>> Handle(
        GetPeakHoursAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, FeatureCode, cancellationToken);
        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(
                request.GymId, cancellationToken);
            return Result.Failure<PeakHoursAnalyticsResponse>(
                PlanErrors.FeatureNotAvailable(FeatureCode, currentPlan, "Pro"));
        }

        var now = DateTime.UtcNow;
        var to = request.To ?? now;
        var from = request.From ?? to.AddDays(-DefaultRangeDays);

        if (from >= to)
            return Result.Failure<PeakHoursAnalyticsResponse>(GymErrors.InvalidCheckIn);

        var result = await _gymQueryService.GetPeakHoursAsync(
            request.GymId, from, to, request.TimezoneOffsetMinutes, cancellationToken);

        if (result is null)
            return Result.Failure<PeakHoursAnalyticsResponse>(GymErrors.NotFound);

        return Result.Success(result);
    }
}

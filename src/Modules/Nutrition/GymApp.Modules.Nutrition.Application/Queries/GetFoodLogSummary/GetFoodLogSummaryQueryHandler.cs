using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetFoodLogSummary;

internal sealed class GetFoodLogSummaryQueryHandler : IQueryHandler<GetFoodLogSummaryQuery, FoodLogSummaryResponse>
{
    private readonly IFoodLogRepository _foodLogRepository;

    public GetFoodLogSummaryQueryHandler(IFoodLogRepository foodLogRepository)
    {
        _foodLogRepository = foodLogRepository;
    }

    public async Task<Result<FoodLogSummaryResponse>> Handle(
        GetFoodLogSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var logs = await _foodLogRepository.GetByUserAsync(
            request.UserId, request.GymId, request.From, request.To, cancellationToken);

        var dailyLogs = logs.Select(log => new DailyNutritionDto(
            log.Date,
            log.DailyTotals.TotalCalories,
            log.DailyTotals.TotalProtein,
            log.DailyTotals.TotalCarbs,
            log.DailyTotals.TotalFat))
            .ToList();

        int totalDays = dailyLogs.Count;

        var response = new FoodLogSummaryResponse(
            request.From,
            request.To,
            totalDays,
            AverageCalories: totalDays > 0 ? dailyLogs.Average(d => d.TotalCalories) : 0,
            AverageProtein: totalDays > 0 ? dailyLogs.Average(d => d.TotalProtein) : 0,
            AverageCarbs: totalDays > 0 ? dailyLogs.Average(d => d.TotalCarbs) : 0,
            AverageFat: totalDays > 0 ? dailyLogs.Average(d => d.TotalFat) : 0,
            DailyLogs: dailyLogs);

        return Result.Success(response);
    }
}

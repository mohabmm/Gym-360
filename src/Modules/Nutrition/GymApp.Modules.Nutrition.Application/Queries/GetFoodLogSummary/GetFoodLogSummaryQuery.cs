using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetFoodLogSummary;

public sealed record GetFoodLogSummaryQuery(
    Guid UserId,
    Guid GymId,
    DateOnly From,
    DateOnly To) : IQuery<FoodLogSummaryResponse>;

using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetMealPlanDetail;

public sealed record GetMealPlanDetailQuery(Guid MealPlanId) : IQuery<MealPlanDetailDto>;

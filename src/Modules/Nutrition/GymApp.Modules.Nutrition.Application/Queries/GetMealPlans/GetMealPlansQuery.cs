using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetMealPlans;

public sealed record GetMealPlansQuery(
    Guid TrainerId,
    Guid GymId) : IQuery<List<MealPlanDto>>;

using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetMyMealPlan;

/// <summary>
/// Returns the trainee's single most-recently-assigned, non-template meal plan
/// at the given gym. Returns a successful result with a null payload when no
/// plan exists (PRD: absence of data is 200 OK with null, never 404).
/// </summary>
public sealed record GetMyMealPlanQuery(Guid UserId, Guid GymId) : IQuery<MealPlanDetailDto?>;

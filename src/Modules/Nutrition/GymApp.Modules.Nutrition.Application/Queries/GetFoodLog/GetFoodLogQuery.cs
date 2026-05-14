using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetFoodLog;

public sealed record GetFoodLogQuery(
    Guid UserId,
    Guid GymId,
    DateOnly Date) : IQuery<FoodLogResponse>;

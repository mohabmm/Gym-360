using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetClientFoodLog;

public sealed record GetClientFoodLogQuery(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    DateOnly Date) : IQuery<FoodLogResponse>;

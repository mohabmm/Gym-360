using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.GetWaterLog;

public sealed record GetWaterLogQuery(
    Guid UserId,
    Guid GymId,
    DateOnly Date) : IQuery<WaterLogResponse>;

using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Queries.SearchFoodDatabase;

public sealed record SearchFoodDatabaseQuery(
    string Query,
    int Page,
    int PageSize) : IQuery<PagedResponse<FoodDatabaseItemDto>>;

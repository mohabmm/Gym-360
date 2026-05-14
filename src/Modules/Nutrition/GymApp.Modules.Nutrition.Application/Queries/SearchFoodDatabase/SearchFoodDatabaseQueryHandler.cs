using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.SearchFoodDatabase;

internal sealed class SearchFoodDatabaseQueryHandler
    : IQueryHandler<SearchFoodDatabaseQuery, PagedResponse<FoodDatabaseItemDto>>
{
    private readonly IFoodDatabaseRepository _foodDatabaseRepository;

    public SearchFoodDatabaseQueryHandler(IFoodDatabaseRepository foodDatabaseRepository)
    {
        _foodDatabaseRepository = foodDatabaseRepository;
    }

    public async Task<Result<PagedResponse<FoodDatabaseItemDto>>> Handle(
        SearchFoodDatabaseQuery request,
        CancellationToken cancellationToken)
    {
        var allResults = await _foodDatabaseRepository.SearchAsync(request.Query, cancellationToken);

        int totalCount = allResults.Count;
        int totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var pagedItems = allResults
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(NutritionMapper.ToFoodDatabaseItemDto)
            .ToList();

        var response = new PagedResponse<FoodDatabaseItemDto>(
            pagedItems,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);

        return Result.Success(response);
    }
}

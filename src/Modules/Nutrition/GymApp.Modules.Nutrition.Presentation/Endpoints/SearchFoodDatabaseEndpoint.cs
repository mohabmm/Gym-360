using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Queries.SearchFoodDatabase;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class SearchFoodDatabaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/nutrition/food-database", async (
            string? query,
            int? page,
            int? pageSize,
            ISender sender) =>
        {
            var searchQuery = new SearchFoodDatabaseQuery(
                query ?? string.Empty,
                page ?? 1,
                pageSize ?? 20);

            var result = await sender.Send(searchQuery);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Search food database")
        .WithDescription("Searches the built-in Egyptian food database by name (supports both Arabic and English). When `query` is empty, returns all active items ordered by name. Returns a paginated list with macro values; defaults to page=1, pageSize=20.")
        .Produces<ApiResponse<PagedResponse<FoodDatabaseItemDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Queries.GetFoodLog;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class GetFoodLogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/nutrition/food-log", async (
            Guid userId,
            Guid gymId,
            DateOnly date,
            ISender sender) =>
        {
            var query = new GetFoodLogQuery(userId, gymId, date);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Get daily food log")
        .WithDescription("Returns the authenticated member's food log for a specific date, including all meal entries and aggregated macro totals.")
        .Produces<ApiResponse<FoodLogResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

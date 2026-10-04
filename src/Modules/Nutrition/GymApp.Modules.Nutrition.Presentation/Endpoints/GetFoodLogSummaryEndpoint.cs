using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Queries.GetFoodLogSummary;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class GetFoodLogSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/nutrition/food-log/summary", async (
            Guid gymId,
            DateOnly from,
            DateOnly to,
            HttpContext http,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(http, out var userId))
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var query = new GetFoodLogSummaryQuery(userId, gymId, from, to);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Get food log summary")
        .WithDescription("Returns a date-range summary of nutrition data including average daily calories, protein, carbs, and fat, along with a per-day breakdown.")
        .Produces<ApiResponse<FoodLogSummaryResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

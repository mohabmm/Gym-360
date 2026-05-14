using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Queries.GetMyMealPlan;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class GetMyMealPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/nutrition/me/meal-plan", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var query = new GetMyMealPlanQuery(userId, gymId.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Get the authenticated trainee's current meal plan at the selected gym")
        .WithDescription("Returns the single most-recently-assigned, non-template meal plan for the authenticated user in the gym specified by the X-Gym-Id header. Returns 200 with data=null when no plan is assigned. Not plan-gated (PDPL).")
        .Produces<ApiResponse<MealPlanDetailDto?>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .RequireAuthorization("trainee");
    }
}

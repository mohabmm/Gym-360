using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.CreateMealPlan;
using GymApp.Modules.Nutrition.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class CreateMealPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/nutrition/meal-plans", async (CreateMealPlanRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new CreateMealPlanCommand(
                trainerId,
                request.GymId,
                request.Name,
                request.Description,
                request.DailyCalorieTarget,
                request.MacroTargets,
                request.Days,
                request.IsTemplate);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Create a meal plan")
        .WithDescription("Creates a new meal plan authored by a trainer. Requires the meal_plans feature to be enabled on the gym's subscription plan. Plans can be marked as reusable templates.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .RequireAuthorization();
    }
}

internal sealed record CreateMealPlanRequest(
    Guid GymId,
    string Name,
    string? Description,
    int DailyCalorieTarget,
    MacroTargetsDto MacroTargets,
    List<MealPlanDayDto> Days,
    bool IsTemplate);

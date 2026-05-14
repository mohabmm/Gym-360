using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.UpdateMealPlan;
using GymApp.Modules.Nutrition.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class UpdateMealPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/nutrition/meal-plans/{mealPlanId:guid}", async (
            Guid mealPlanId,
            UpdateMealPlanRequest request,
            ISender sender) =>
        {
            var command = new UpdateMealPlanCommand(
                mealPlanId,
                request.Name,
                request.Description,
                request.DailyCalorieTarget,
                request.MacroTargets,
                request.Days);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Update meal plan")
        .WithDescription("Updates an existing meal plan's name, description, calorie/macro targets, and day-level meal entries. All fields are optional — only provided fields are updated.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record UpdateMealPlanRequest(
    string? Name,
    string? Description,
    int? DailyCalorieTarget,
    MacroTargetsDto? MacroTargets,
    List<MealPlanDayDto>? Days);

using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.AssignMealPlan;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class AssignMealPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/nutrition/meal-plans/{mealPlanId:guid}/assign", async (
            Guid mealPlanId,
            AssignMealPlanRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new AssignMealPlanCommand(mealPlanId, trainerId, request.ClientId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Assign meal plan to client")
        .WithDescription("Assigns an existing meal plan created by a trainer to one of their clients.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record AssignMealPlanRequest(
    Guid ClientId);

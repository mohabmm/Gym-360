using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.DeleteFoodItem;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class DeleteFoodItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/nutrition/food-log/meals/{mealId:guid}/items/{index:int}", async (
            Guid mealId,
            int index,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new DeleteFoodItemCommand(userId, mealId, index);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Delete a logged food item")
        .WithDescription("Removes a single food item from a logged meal by its index. If the meal has no items left, the empty meal is removed too.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

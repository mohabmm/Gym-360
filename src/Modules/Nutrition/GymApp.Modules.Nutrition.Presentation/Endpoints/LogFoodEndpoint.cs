using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.LogFood;
using GymApp.Modules.Nutrition.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class LogFoodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/nutrition/food-log", async (LogFoodRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new LogFoodCommand(
                userId,
                request.GymId,
                request.Date,
                request.MealType,
                request.FoodItems);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Log food intake")
        .WithDescription("Logs a meal entry for a member on a specific date, including all food items consumed and their macro values.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

internal sealed record LogFoodRequest(
    Guid GymId,
    DateOnly Date,
    string MealType,
    List<FoodItemDto> FoodItems);

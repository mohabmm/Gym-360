using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Queries.GetClientFoodLog;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class GetClientFoodLogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/nutrition/clients/{clientId:guid}/food-log", async (
            Guid clientId,
            Guid trainerId,
            Guid gymId,
            DateOnly date,
            ISender sender) =>
        {
            var query = new GetClientFoodLogQuery(trainerId, clientId, gymId, date);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Get client food log")
        .WithDescription("Returns a trainer's view of a client's food log for a specific date, including all meal entries and macro totals.")
        .Produces<ApiResponse<FoodLogResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

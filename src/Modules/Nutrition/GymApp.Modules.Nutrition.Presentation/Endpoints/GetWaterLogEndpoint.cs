using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Queries.GetWaterLog;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class GetWaterLogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/nutrition/water-log", async (
            Guid userId,
            Guid gymId,
            DateOnly date,
            ISender sender) =>
        {
            var query = new GetWaterLogQuery(userId, gymId, date);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Get daily water log")
        .WithDescription("Returns the authenticated member's water intake log for a specific date, including individual entries and total vs. target millilitres.")
        .Produces<ApiResponse<WaterLogResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

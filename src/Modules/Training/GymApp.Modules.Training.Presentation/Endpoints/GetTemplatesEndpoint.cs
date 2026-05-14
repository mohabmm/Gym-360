using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetTemplates;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetTemplatesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/templates", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymIdHeader,
            [FromQuery] Guid? gymId,
            ISender sender) =>
        {
            var resolvedGymId = gymId ?? gymIdHeader ?? UserContext.TryGetGymId(httpContext);
            if (resolvedGymId is null || resolvedGymId == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header or gymId query parameter is required.",
                    400);

            var query = new GetTemplatesQuery(resolvedGymId.Value);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("List workout templates for a gym")
        .WithDescription("Returns all workout templates available within the specified gym. Gym context comes from the X-Gym-Id header or a gymId query parameter.")
        .Produces<ApiResponse<List<WorkoutDto>>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}

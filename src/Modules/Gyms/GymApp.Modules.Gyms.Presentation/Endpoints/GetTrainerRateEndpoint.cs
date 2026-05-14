using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetTrainerRate;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetTrainerRateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/rate",
            async (Guid gymId, Guid trainerId, ISender sender) =>
            {
                var query = new GetTrainerRateQuery(gymId, trainerId);
                var result = await sender.Send(query);

                return result.IsSuccess
                    ? ApiResults.Ok(result.Value)
                    : ApiResults.Problem(result.Error);
            })
        .WithTags("Gyms")
        .WithSummary("Get trainer session rate")
        .WithDescription("Returns the configured per-session EGP rate for a trainer at a specific gym.")
        .Produces<ApiResponse<TrainerRateDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}

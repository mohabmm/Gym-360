using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetSeasonalHours;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetSeasonalHoursEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/seasonal-hours", async (Guid gymId, ISender sender) =>
        {
            var result = await sender.Send(new GetSeasonalHoursQuery(gymId));
            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get gym seasonal hour overrides")
        .Produces<ApiResponse<List<GymSeasonalHoursDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetGymEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}", async (Guid gymId, ISender sender) =>
        {
            var query = new GetGymQuery(gymId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get gym by ID")
        .WithDescription("Returns full gym details including branding, address, contact, and member/trainer counts.")
        .Produces<ApiResponse<GymResponse>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}

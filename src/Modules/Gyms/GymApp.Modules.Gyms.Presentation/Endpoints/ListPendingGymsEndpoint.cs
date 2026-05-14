using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.ListPendingGyms;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ListPendingGymsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/pending", async (ISender sender) =>
        {
            var result = await sender.Send(new ListPendingGymsQuery());

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("List gyms awaiting platform-admin approval")
        .WithDescription("Returns all gyms currently in PendingApproval status, ordered by creation date.")
        .Produces<ApiResponse<List<GymResponse>>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}

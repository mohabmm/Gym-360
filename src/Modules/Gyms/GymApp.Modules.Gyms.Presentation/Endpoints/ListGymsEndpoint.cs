using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.ListGyms;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ListGymsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms", async (
            int? page,
            int? pageSize,
            string? name,
            ISender sender) =>
        {
            var query = new ListGymsQuery(page ?? 1, pageSize ?? 20, name);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("List gyms (paginated, filterable by name)")
        .WithDescription("Returns a paginated list of gyms. Supports basic name filter. Defaults: page=1, pageSize=20 (max 100).")
        .Produces<ApiResponse<PagedResponse<GymResponse>>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}

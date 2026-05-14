using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetTrainerClients;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetTrainerClientsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/clients",
            async (Guid gymId, Guid trainerId, ISender sender) =>
            {
                var query = new GetTrainerClientsQuery(gymId, trainerId);
                var result = await sender.Send(query);

                return result.IsSuccess
                    ? ApiResults.Ok(result.Value)
                    : ApiResults.Problem(result.Error);
            })
        .WithTags("Gyms")
        .WithSummary("List trainer's active clients")
        .WithDescription("Returns members with an active session package booked against this trainer at the gym.")
        .Produces<ApiResponse<List<MemberDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetGymClassById;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetGymClassByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/classes/{classId:guid}", async (
            Guid gymId,
            Guid classId,
            ISender sender) =>
        {
            var query = new GetGymClassByIdQuery(gymId, classId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get a gym class by id")
        .WithDescription("Returns a single gym class with schedule, capacity, trainer, booking count, and status.")
        .Produces<ApiResponse<GymClassDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}

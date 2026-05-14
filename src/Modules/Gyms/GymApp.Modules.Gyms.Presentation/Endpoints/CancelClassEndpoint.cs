using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.CancelClass;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class CancelClassEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/gyms/{gymId:guid}/classes/{classId:guid}", async (
            Guid gymId,
            Guid classId,
            ISender sender) =>
        {
            var command = new CancelClassCommand(gymId, classId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Cancel (soft-delete) a gym class")
        .WithDescription("Marks the class as Cancelled. Existing bookings are preserved for audit. The class no longer appears in the active class list.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}

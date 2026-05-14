using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.ExportMyData;
using GymApp.Modules.Identity.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class ExportMyDataEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/identity/my-data", async (HttpContext httpContext, ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new ExportMyDataCommand(userId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity", "PDPL")
        .WithSummary("Export all personal data (PDPL)")
        .WithDescription("Generates a downloadable export of all personal data held for the authenticated user, as required by Egypt's PDPL. User id is derived from the JWT; clients cannot request data for another user.")
        .Produces<ApiResponse<DataExportResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.DeleteAccount;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class DeleteAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/identity/account", async (HttpContext httpContext, ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new DeleteAccountCommand(userId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity", "PDPL")
        .WithSummary("Delete user account (PDPL)")
        .WithDescription("Permanently deletes the authenticated user's account and all associated personal data, as required by Egypt's PDPL right to erasure. User id is derived from the JWT; clients cannot target another user.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

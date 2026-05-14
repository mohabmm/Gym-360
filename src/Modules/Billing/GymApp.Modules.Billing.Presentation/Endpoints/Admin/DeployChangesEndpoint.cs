using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Deploy;
using GymApp.Modules.Billing.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class DeployChangesEndpoint : IEndpoint
{
    // Body is optional; if a client passes DeployedBy explicitly we honor it, otherwise we
    // derive the deployer from the JWT 'sub' claim so the client never needs to know its own
    // user id.
    internal sealed record DeployChangesRequest(Guid? DeployedBy);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/deploy/{scope}/{targetId:guid?}", async (
            string scope,
            Guid? targetId,
            DeployChangesRequest? request,
            HttpContext httpContext,
            ISender sender) =>
        {
            Guid deployedBy = request?.DeployedBy ?? Guid.Empty;
            if (deployedBy == Guid.Empty)
            {
                var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? httpContext.User.FindFirstValue("sub");
                if (Guid.TryParse(sub, out var fromJwt))
                    deployedBy = fromJwt;
            }

            var command = new DeployChangesCommand(scope, targetId, deployedBy);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Deploy plan changes")
        .WithDescription("Triggers FCM push and SignalR config_updated events to propagate plan or feature changes to gyms. Requires platform_admin role. The deployer is derived from the JWT.")
        .WithTags("Billing")
        .Produces<ApiResponse<DeployResult>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

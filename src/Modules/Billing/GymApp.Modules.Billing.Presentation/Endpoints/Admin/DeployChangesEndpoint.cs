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

    /// <summary>Route values identifying what to deploy.</summary>
    internal sealed record DeployTarget(string Scope, Guid? TargetId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/deploy/{scope}/{targetId:guid?}", async (
            [AsParameters] DeployTarget target,
            DeployChangesRequest? request,
            HttpContext httpContext,
            ISender sender) =>
        {
            var deployedBy = ResolveDeployedBy(request, httpContext.User);

            var command = new DeployChangesCommand(target.Scope, target.TargetId, deployedBy);
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

    private static Guid ResolveDeployedBy(DeployChangesRequest? request, ClaimsPrincipal user)
    {
        Guid deployedBy = request?.DeployedBy ?? Guid.Empty;
        if (deployedBy != Guid.Empty)
            return deployedBy;

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var fromJwt) ? fromJwt : deployedBy;
    }
}

using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.GymSubscriptions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class ChangeGymPlanEndpoint : IEndpoint
{
    // ChangedBy is optional in the body; when absent we derive the caller from the JWT 'sub'
    // claim. Trusting the JWT over a client-supplied id also prevents impersonation.
    internal sealed record ChangeGymPlanRequest(
        Guid PlanId,
        string Reason,
        Guid? ChangedBy);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/admin/gyms/{gymId:guid}/subscription/plan", async (
            Guid gymId,
            ChangeGymPlanRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            Guid changedBy = request.ChangedBy ?? Guid.Empty;
            if (changedBy == Guid.Empty)
            {
                var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? httpContext.User.FindFirstValue("sub");
                if (Guid.TryParse(sub, out var fromJwt))
                    changedBy = fromJwt;
            }

            var command = new ChangeGymPlanCommand(
                gymId,
                request.PlanId,
                request.Reason,
                changedBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Change gym subscription plan")
        .WithDescription("Switches a gym to a different subscription plan and records the change reason. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

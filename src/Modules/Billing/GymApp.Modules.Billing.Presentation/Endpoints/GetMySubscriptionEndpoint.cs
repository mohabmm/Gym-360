using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.GymSubscriptions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints;

// Self-read variant of GetGymSubscription. Owner / staff principals at a gym pull the
// gym's current subscription detail. Tenant scope is implicit from the JWT gymId claim;
// no path parameter required, no platform_admin role check.
internal sealed class GetMySubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/my-subscription", async (
            HttpContext httpContext,
            ISender sender) =>
        {
            var gymClaim = httpContext.User.FindFirstValue("gymId");
            if (!Guid.TryParse(gymClaim, out var gymId) || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED",
                    "JWT does not carry a gymId claim. Owners must call /identity/select-gym first.", 400);

            var query = new GetGymSubscriptionQuery(gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get current gym's subscription (self)")
        .WithDescription("Owner/staff/trainer/trainee fetches the subscription details for the gym in their JWT gymId claim.")
        .WithTags("Billing")
        .Produces<ApiResponse<GymSubscriptionDetailDto>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}

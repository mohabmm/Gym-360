using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.PlanFeatures;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.PlanFeatures;

internal sealed class GetPlanFeaturesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Accepts the gym either via the X-Gym-Id header (mobile convention —
        // GymIdInterceptor injects it on every request) or via ?gymId= query
        // (legacy / admin callers). Header takes priority when both are present.
        app.MapGet("api/v1/billing/plan-features", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            [FromQuery(Name = "gymId")] Guid? queryGymId,
            ISender sender) =>
        {
            var resolved = headerGymId ?? queryGymId;
            if (resolved is null || resolved == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var query = new GetPlanFeaturesQuery(resolved.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get active features for a gym")
        .WithDescription("Returns all features enabled for the gym's current plan, including any per-gym overrides. Pass the gym via X-Gym-Id header (preferred) or ?gymId= query param.")
        .WithTags("Billing")
        .Produces<ApiResponse<PlanFeaturesResponse>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}

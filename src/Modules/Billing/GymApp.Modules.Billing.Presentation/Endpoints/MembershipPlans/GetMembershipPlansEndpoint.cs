using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.MembershipPlans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.MembershipPlans;

internal sealed class GetMembershipPlansEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/plans", async (
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

            var query = new GetMembershipPlansQuery(resolved.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get gym membership plans")
        .WithDescription("Returns all active membership plans available for a gym, used to enroll new members.")
        .WithTags("Billing")
        .Produces<ApiResponse<List<MembershipPlanDto>>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}

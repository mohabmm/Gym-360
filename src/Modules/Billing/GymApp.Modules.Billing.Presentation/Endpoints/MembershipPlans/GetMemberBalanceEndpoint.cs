using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.MemberBalance;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.MembershipPlans;

internal sealed class GetMemberBalanceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/members/{memberId:guid}/balance", async (
            Guid memberId,
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

            var query = new GetMemberBalanceQuery(resolved.Value, memberId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        // Staff-only: a member's balance/memberships/package summary is gym-staff/owner data.
        .RequireAuthorization("gym_staff")
        .WithSummary("Get member billing balance")
        .WithDescription("Returns a member's current balance, active memberships, and session package summary for a given gym.")
        .WithTags("Billing")
        .Produces<ApiResponse<MemberBalanceResponse>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}

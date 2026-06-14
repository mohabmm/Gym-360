using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.Payments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Payments;

internal sealed class GetMemberPaymentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/members/{memberId:guid}/payments", async (
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

            var query = new GetMemberPaymentsQuery(resolved.Value, memberId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get a member's payments")
        .WithDescription("Returns all payments for a single member (no pagination).")
        .WithTags("Billing")
        .Produces<ApiResponse<List<PaymentDto>>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}

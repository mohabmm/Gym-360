using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.Payments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Payments;

internal sealed class GetMyPaymentsEndpoint : IEndpoint
{
    private static readonly HashSet<string> AllowedPurposes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Membership", "SessionPackage", "Product", "All"
    };

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/me/payments", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            ISender sender,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] string? purpose,
            [FromQuery] int? page,
            [FromQuery] int? pageSize) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var effectivePurpose = string.IsNullOrWhiteSpace(purpose) ? "All" : purpose;
            if (!AllowedPurposes.Contains(effectivePurpose))
                return ApiResults.Problem("VALIDATION_ERROR",
                    "purpose must be one of Membership, SessionPackage, Product, All.", 400);

            if (from is not null && to is not null && from > to)
                return ApiResults.Problem("VALIDATION_ERROR",
                    "'from' must be on or before 'to'.", 400);

            var query = new GetMyPaymentsQuery(
                userId,
                gymId.Value,
                from,
                to,
                effectivePurpose,
                page ?? 1,
                pageSize ?? 20);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Billing")
        .WithSummary("Get the authenticated trainee's payment history at the selected gym")
        .WithDescription("Returns the trainee's payments (including Voided) ordered by PaidAt DESC, filtered by date range and purpose, paginated. Not plan-gated (PDPL).")
        .Produces<ApiResponse<PagedResponse<MyPaymentDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .RequireAuthorization("trainee");
    }
}

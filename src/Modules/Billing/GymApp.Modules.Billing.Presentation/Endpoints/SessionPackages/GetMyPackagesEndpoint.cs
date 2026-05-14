using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.SessionPackages;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.SessionPackages;

internal sealed class GetMyPackagesEndpoint : IEndpoint
{
    private static readonly HashSet<string> AllowedStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active", "Exhausted", "Expired", "All"
    };

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/me/packages", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            ISender sender,
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var effectiveStatus = status;
            if (string.IsNullOrWhiteSpace(effectiveStatus))
                effectiveStatus = "Active";
            else if (!AllowedStatus.Contains(effectiveStatus))
                return ApiResults.Problem("VALIDATION_ERROR",
                    "status must be one of Active, Exhausted, Expired, All.", 400);

            var query = new GetMyPackagesQuery(
                userId,
                gymId.Value,
                effectiveStatus,
                page ?? 1,
                pageSize ?? 20);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Billing")
        .WithSummary("Get the authenticated trainee's session packages at the selected gym")
        .WithDescription("Returns the trainee's session packages filtered by status (Active by default), paginated. Not plan-gated (PDPL).")
        .Produces<ApiResponse<PagedResponse<MySessionPackageDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .RequireAuthorization("trainee");
    }
}

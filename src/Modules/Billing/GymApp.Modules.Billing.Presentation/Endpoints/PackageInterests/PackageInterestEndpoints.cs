using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Billing.Application.Commands.PackageInterests;
using GymApp.Modules.Billing.Application.Queries.PackageInterests;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.PackageInterests;

/// <summary>
/// pt.package_interest:
/// <c>POST /api/v1/billing/package-interest</c> (trainee taps "I'm interested"),
/// <c>GET /api/v1/billing/package-interest?state=pending</c> and
/// <c>POST /api/v1/billing/package-interest/{id}/dismiss</c> (desk).
/// </summary>
internal sealed class PackageInterestEndpoints : IEndpoint
{
    internal sealed record ExpressInterestRequest(Guid SourcePackageId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/package-interest", async (
            ExpressInterestRequest request,
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var memberId))
                return Results.Unauthorized();

            var gymId = GymContext.Resolve(httpContext, headerGymId);
            if (gymId is null)
                return GymContext.Required();

            var result = await sender.Send(new ExpressPackageInterestCommand(gymId.Value, memberId, request.SourcePackageId));
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("trainee")
        .RequireFeature(FeatureKeys.PtPackageInterest)
        .WithTags("Billing")
        .WithSummary("Trainee taps \"I'm interested\" on a trial or a low/used-up package; the desk and coach are notified")
        .WithDescription("Idempotent: while the trainee already has a pending lead, that lead is returned with alreadyPending=true.")
        .Produces<ApiResponse<PackageInterestReceipt>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        app.MapGet("api/v1/billing/package-interest", async (
            [FromQuery] string? state,
            HttpContext httpContext,
            ISender sender) =>
        {
            var gymId = UserContext.TryGetGymId(httpContext);
            if (gymId is null)
                return GymContext.Required();

            var effective = string.IsNullOrWhiteSpace(state) ? PackageInterestStates.Pending : state;
            if (!PackageInterestStates.Known.Contains(effective))
                return ApiResults.Problem("VALIDATION_ERROR", "state must be one of pending, converted, dismissed, all.", 400);

            var result = await sender.Send(new GetPackageInterestsQuery(gymId.Value, effective));
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_staff")
        .RequireFeature(FeatureKeys.PtPackageInterest)
        .WithTags("Billing")
        .WithSummary("Desk lead list: trainees who tapped \"I'm interested\" (pending first, oldest first)")
        .Produces<ApiResponse<IReadOnlyList<PackageInterestDto>>>(StatusCodes.Status200OK);

        app.MapPost("api/v1/billing/package-interest/{id:guid}/dismiss", async (
            Guid id,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var staffUserId))
                return Results.Unauthorized();

            var gymId = UserContext.TryGetGymId(httpContext);
            if (gymId is null)
                return GymContext.Required();

            var result = await sender.Send(new DismissPackageInterestCommand(gymId.Value, id, staffUserId));
            return result.IsSuccess ? ApiResults.Ok(new { id }) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_staff")
        .RequireFeature(FeatureKeys.PtPackageInterest)
        .WithTags("Billing")
        .WithSummary("Desk closes a lead that will not convert")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }
}

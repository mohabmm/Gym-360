using System.Security.Claims;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Billing.Application.Queries.PtReports;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.PtReports;

/// <summary>
/// PT reports for gym staff:
/// <c>GET /api/v1/billing/trials/not-converted</c> (desk, pt.trial_end_alerts) and
/// <c>GET /api/v1/billing/pt-dashboard</c> (owner/manager, pt.owner_dashboard).
/// </summary>
internal sealed class PtReportEndpoints : IEndpoint
{
    private const int DefaultPeriodDays = 30;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/trials/not-converted", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            var gymId = ResolveGymId(httpContext, headerGymId);
            if (gymId is null)
                return GymRequired();

            var result = await sender.Send(new GetNotConvertedTrialsQuery(gymId.Value));
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_staff")
        .RequireFeature(FeatureKeys.PtTrialEndAlerts)
        .WithTags("Billing")
        .WithSummary("Trainees who finished a free trial and have not bought a package")
        .Produces<ApiResponse<IReadOnlyList<NotConvertedTrialDto>>>(StatusCodes.Status200OK);

        app.MapGet("api/v1/billing/pt-dashboard", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            var gymId = ResolveGymId(httpContext, headerGymId);
            if (gymId is null)
                return GymRequired();

            var toUtc = (to ?? DateTime.UtcNow).ToUniversalTime();
            var fromUtc = (from ?? toUtc.AddDays(-DefaultPeriodDays)).ToUniversalTime();
            var result = await sender.Send(new GetPtDashboardQuery(gymId.Value, fromUtc, toUtc));
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_owner_or_manager")
        .RequireFeature(FeatureKeys.PtOwnerDashboard)
        .WithTags("Billing")
        .WithSummary("PT owner dashboard: trial-to-paid conversion per coach, renewals, sessions logged")
        .WithDescription("Period defaults to the last 30 days. Converted = paid package (any coach) within 14 days after the trial ended, credited to the trial coach.")
        .Produces<ApiResponse<PtDashboardDto>>(StatusCodes.Status200OK);
    }

    // The gym comes only from the token's selected gym (never a client header), so staff and
    // owners can only read the gym they are signed into.
    private static Guid? ResolveGymId(HttpContext httpContext, Guid? headerGymId) =>
        Guid.TryParse(httpContext.User.FindFirstValue("gymId"), out var fromClaim) ? fromClaim : null;

    private static IResult GymRequired() =>
        ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (/api/v1/identity/select-gym).", StatusCodes.Status400BadRequest);
}

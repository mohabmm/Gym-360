using System.Security.Claims;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class OwnerDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/owners/{ownerId:guid}/dashboard", async (
            Guid ownerId,
            HttpContext httpContext,
            IOwnerDashboardService dashboard,
            CancellationToken ct) =>
        {
            // Owners can only read their own rollup. Platform admins can read any.
            var callerOwnerId = httpContext.User.FindFirstValue("ownerId");
            var adminRole = httpContext.User.FindFirstValue("adminRole");

            if (string.IsNullOrEmpty(adminRole))
            {
                if (!Guid.TryParse(callerOwnerId, out var parsed) || parsed != ownerId)
                    return Results.Forbid();
            }

            var result = await dashboard.GetAsync(ownerId, ct);
            return Results.Ok(result);
        })
        .WithTags("Tenancy")
        .WithSummary("Owner unified dashboard (cross-gym rollup)")
        .WithDescription("Aggregates active members, trainers, last-30-days revenue and sessions across every gym this Owner runs. Cached 5 minutes.")
        .RequireAuthorization();
    }
}

using GymApp.Modules.Billing.Shared;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Settings;

internal sealed class GetTestingModeEndpoint : IEndpoint
{
    internal sealed record Response(bool Enabled);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/settings/testing-mode", async (
            Guid gymId,
            IGymPlanService plans,
            CancellationToken cancellationToken) =>
        {
            var enabled = await plans.IsFeatureEnabledAsync(gymId, "early_session_start", cancellationToken);
            return ApiResults.Ok(new Response(enabled));
        })
        .RequireAuthorization()
        .WithTags("Gyms")
        .WithSummary("Read gym testing mode flag")
        .WithDescription("Returns whether the early_session_start feature override is enabled for this gym. The trainer mobile app polls this on launch and reflects the value as a settings toggle.")
        .Produces<ApiResponse<Response>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.PlanFeatures;
using GymApp.Modules.Billing.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class BulkUpdatePlanFeaturesEndpoint : IEndpoint
{
    internal sealed record BulkUpdatePlanFeaturesRequest(List<PlanFeatureToggleDto> Features);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/admin/plans/{planId:guid}/features", async (
            Guid planId,
            BulkUpdatePlanFeaturesRequest request,
            ISender sender) =>
        {
            var command = new BulkUpdatePlanFeaturesCommand(planId, request.Features);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Bulk update plan features")
        .WithDescription("Replaces all feature toggles for a subscription plan in one call. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

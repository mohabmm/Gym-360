using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.PlanFeatures;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class TogglePlanFeatureEndpoint : IEndpoint
{
    internal sealed record TogglePlanFeatureRequest(
        bool IsEnabled,
        int? LimitValue);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/admin/plans/{planId:guid}/features/{featureId:guid}", async (
            Guid planId,
            Guid featureId,
            TogglePlanFeatureRequest request,
            ISender sender) =>
        {
            var command = new TogglePlanFeatureCommand(
                planId,
                featureId,
                request.IsEnabled,
                request.LimitValue);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Toggle a feature on a plan")
        .WithDescription("Enables or disables a single feature for a subscription plan, with an optional limit value. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

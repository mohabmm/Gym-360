using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class RemoveGymFeatureOverrideEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/admin/gyms/{gymId:guid}/feature-overrides/{featureId:guid}", async (
            Guid gymId,
            Guid featureId,
            ISender sender) =>
        {
            var command = new RemoveGymFeatureOverrideCommand(gymId, featureId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Remove gym feature override")
        .WithDescription("Removes a per-gym feature override so the feature reverts to the plan default. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

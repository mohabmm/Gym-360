using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.GymFeatureOverrides;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class GetGymFeatureOverridesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/gyms/{gymId:guid}/feature-overrides", async (
            Guid gymId,
            ISender sender) =>
        {
            var query = new GetGymFeatureOverridesQuery(gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Get gym feature overrides")
        .WithDescription("Returns all per-gym feature overrides that differ from the gym's plan defaults. Requires platform_admin role.")
        .WithTags("Billing")
        .Produces<ApiResponse<List<GymFeatureOverrideDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

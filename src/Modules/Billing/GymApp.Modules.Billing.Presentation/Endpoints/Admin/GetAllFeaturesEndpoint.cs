using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.Features;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class GetAllFeaturesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/features", async (ISender sender) =>
        {
            var query = new GetAllFeaturesQuery();
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("List all features")
        .WithDescription("Returns all features defined on the platform (active and inactive). Requires platform_admin role.")
        .WithTags("Billing")
        .Produces<ApiResponse<List<FeatureDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.Plans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class GetAllPlansEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/plans", async (ISender sender) =>
        {
            var query = new GetAllPlansQuery();
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Get all subscription plans")
        .WithDescription("Returns all subscription plans including inactive ones. Requires platform_admin role.")
        .WithTags("Billing")
        .Produces<ApiResponse<List<SubscriptionPlanDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

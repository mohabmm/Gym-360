using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.PlanChangeLogs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class GetPlanChangeLogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/plan-change-log", async (
            Guid? gymId,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetPlanChangeLogQuery(gymId, page, pageSize);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Get plan change audit log")
        .WithDescription("Returns a paginated log of all subscription plan changes, optionally filtered by gym. Requires platform_admin role.")
        .WithTags("Billing")
        .Produces<ApiResponse<PagedResponse<PlanChangeLogDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

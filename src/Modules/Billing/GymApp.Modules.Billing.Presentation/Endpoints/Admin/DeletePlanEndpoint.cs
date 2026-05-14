using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Plans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class DeletePlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/admin/plans/{planId:guid}", async (Guid planId, ISender sender) =>
        {
            var command = new DeletePlanCommand(planId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Delete (deactivate) a plan")
        .WithDescription("Soft-deletes the plan by setting IsActive=false. Existing gym subscriptions referencing this plan are preserved. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

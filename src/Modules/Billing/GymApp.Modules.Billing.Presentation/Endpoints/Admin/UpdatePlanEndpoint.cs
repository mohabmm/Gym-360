using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Plans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class UpdatePlanEndpoint : IEndpoint
{
    internal sealed record UpdatePlanRequest(
        string? Name,
        string? Description,
        decimal? MonthlyPrice,
        decimal? AnnualPrice,
        int? MemberLimit,
        int? TrainerLimit,
        int? StaffLimit,
        bool? IsPublic,
        bool? IsActive,
        int? SortOrder);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/admin/plans/{planId:guid}", async (
            Guid planId,
            UpdatePlanRequest request,
            ISender sender) =>
        {
            var command = new UpdatePlanCommand(
                planId,
                request.Name,
                request.Description,
                request.MonthlyPrice,
                request.AnnualPrice,
                request.MemberLimit,
                request.TrainerLimit,
                request.StaffLimit,
                request.IsPublic,
                request.IsActive,
                request.SortOrder);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Update a subscription plan")
        .WithDescription("Updates pricing, limits, visibility, or active status of a subscription plan. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

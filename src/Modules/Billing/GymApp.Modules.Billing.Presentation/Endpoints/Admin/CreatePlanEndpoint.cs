using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Plans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class CreatePlanEndpoint : IEndpoint
{
    internal sealed record CreatePlanRequest(
        string Code,
        string Name,
        string? Description,
        decimal MonthlyPrice,
        decimal AnnualPrice,
        int MemberLimit,
        int TrainerLimit,
        int StaffLimit,
        bool IsPublic,
        int SortOrder);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/plans", async (
            CreatePlanRequest request,
            ISender sender) =>
        {
            var command = new CreatePlanCommand(
                request.Code,
                request.Name,
                request.Description,
                request.MonthlyPrice,
                request.AnnualPrice,
                request.MemberLimit,
                request.TrainerLimit,
                request.StaffLimit,
                request.IsPublic,
                request.SortOrder);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/admin/plans/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Create a subscription plan")
        .WithDescription("Creates a new subscription plan with pricing and member/trainer limits. Requires platform_admin role.")
        .WithTags("Billing")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

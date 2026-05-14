using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.MembershipPlans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.MembershipPlans;

internal sealed class UpdateMembershipPlanEndpoint : IEndpoint
{
    internal sealed record Request(
        string? Name,
        int? DurationDays,
        decimal? Price,
        bool? IsActive,
        List<string>? Features);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/billing/plans/{id:guid}", async (Guid id, Request request, ISender sender) =>
        {
            var command = new UpdateMembershipPlanCommand(
                id, request.Name, request.DurationDays, request.Price, request.IsActive, request.Features);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Update a membership plan")
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}

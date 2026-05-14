using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.MembershipPlans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.MembershipPlans;

internal sealed class CreateMembershipPlanEndpoint : IEndpoint
{
    internal sealed record CreateMembershipPlanRequest(
        Guid GymId,
        string Name,
        int DurationDays,
        decimal Price,
        List<string>? Features);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/plans", async (
            CreateMembershipPlanRequest request,
            ISender sender) =>
        {
            var command = new CreateMembershipPlanCommand(
                request.GymId,
                request.Name,
                request.DurationDays,
                request.Price,
                request.Features);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/billing/plans/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Create a gym membership plan")
        .WithDescription("Creates a new membership plan for a gym, defining duration and pricing for member subscriptions.")
        .WithTags("Billing")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.SessionPackages;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.SessionPackages;

internal sealed class AssignSessionPackageEndpoint : IEndpoint
{
    internal sealed record AssignSessionPackageRequest(
        Guid GymId,
        Guid TrainerId,
        Guid MemberId,
        Guid TemplateId,
        Guid AssignedBy,
        DateTime? ExpiresAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/packages/assign", async (
            AssignSessionPackageRequest request,
            ISender sender) =>
        {
            var command = new AssignSessionPackageCommand(
                request.GymId,
                request.TrainerId,
                request.MemberId,
                request.TemplateId,
                request.AssignedBy,
                request.ExpiresAt);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/billing/packages/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_staff")
        .WithSummary("Assign session package to member")
        .WithDescription("Creates a session package for a member based on a template, linking the trainer and optional expiry date.")
        .WithTags("Billing")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}

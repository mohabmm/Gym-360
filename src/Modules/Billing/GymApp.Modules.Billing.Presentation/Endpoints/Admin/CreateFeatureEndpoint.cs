using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Features;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class CreateFeatureEndpoint : IEndpoint
{
    internal sealed record CreateFeatureRequest(
        string Code,
        string Name,
        string? Description,
        string? Category);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/features", async (
            CreateFeatureRequest request,
            ISender sender) =>
        {
            var command = new CreateFeatureCommand(
                request.Code,
                request.Name,
                request.Description,
                request.Category);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/admin/features/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Create a platform feature")
        .WithDescription("Creates a new feature code that can be assigned to subscription plans. Requires platform_admin role.")
        .WithTags("Billing")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

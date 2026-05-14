using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Features;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class UpdateFeatureEndpoint : IEndpoint
{
    internal sealed record UpdateFeatureRequest(
        string? Name,
        string? Description,
        string? Category,
        bool? IsActive);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/admin/features/{featureId:guid}", async (
            Guid featureId,
            UpdateFeatureRequest request,
            ISender sender) =>
        {
            var command = new UpdateFeatureCommand(
                featureId,
                request.Name,
                request.Description,
                request.Category,
                request.IsActive);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Update a platform feature")
        .WithDescription("Updates the display name, description, category, or active status of a feature. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateBranding;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateBrandingEndpoint : IEndpoint
{
    internal sealed record Request(
        string? LogoUrl,
        string? PrimaryColor,
        string? SecondaryColor,
        string? CoverPhotoUrl);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/branding", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new UpdateBrandingCommand(
                gymId,
                request.LogoUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.CoverPhotoUrl);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update gym branding")
        .WithDescription("Updates gym branding assets including logo URL, primary and secondary colors, and cover photo. All fields are optional.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        // Any gym staff role (owner, manager, receptionist) can update branding.
        // Plan-level access is enforced by the gym_branding feature gate in the handler.
        .RequireAuthorization("gym_staff");
    }
}

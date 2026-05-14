using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class UpdateProfileEndpoint : IEndpoint
{
    // UserId removed from body — derived from JWT 'sub'. Users can only update their own profile.
    internal sealed record Request(
        string? FirstName,
        string? LastName,
        string? Phone,
        string? ProfilePhotoUrl,
        string? DateOfBirth,
        string? Gender,
        string? Language);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/identity/profile", async (Request request, HttpContext http, ISender sender) =>
        {
            var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var command = new UpdateProfileCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.ProfilePhotoUrl,
                request.DateOfBirth,
                request.Gender,
                request.Language);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Update the authenticated user's profile")
        .WithDescription("Partially updates profile fields (name, phone, photo, date of birth, gender, language preference). All fields are optional.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

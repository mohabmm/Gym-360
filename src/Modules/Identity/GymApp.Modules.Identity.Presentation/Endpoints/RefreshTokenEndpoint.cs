using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.RefreshToken;
using GymApp.Modules.Identity.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class RefreshTokenEndpoint : IEndpoint
{
    // RefreshToken matches the field name returned by /identity/login (TokenResponse.refreshToken),
    // so clients can pass it through verbatim. `Token` kept as an alias for backwards compat.
    internal sealed record Request(string? RefreshToken, string? Token);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/refresh-token", async (Request request, ISender sender) =>
        {
            var command = new RefreshTokenCommand(request.RefreshToken ?? request.Token ?? string.Empty);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Refresh JWT access token")
        .WithDescription("Exchanges a valid refresh token for a new JWT access token and refresh token pair.")
        .Produces<ApiResponse<TokenResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .AllowAnonymous();
    }
}

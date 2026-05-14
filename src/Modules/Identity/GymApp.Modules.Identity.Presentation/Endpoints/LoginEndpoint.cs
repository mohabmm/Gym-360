using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.Login;
using GymApp.Modules.Identity.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class LoginEndpoint : IEndpoint
{
    internal sealed record Request(string Email, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/login", async (Request request, ISender sender) =>
        {
            var command = new LoginCommand(request.Email, request.Password);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Authenticate user and return tokens")
        .WithDescription("Validates email and password credentials, and returns a JWT access token and refresh token on success.")
        .Produces<ApiResponse<TokenResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .AllowAnonymous();
    }
}

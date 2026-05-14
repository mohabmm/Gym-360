using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.ForgotPassword;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class ForgotPasswordEndpoint : IEndpoint
{
    internal sealed record Request(string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/forgot-password", async (Request request, ISender sender) =>
        {
            var command = new ForgotPasswordCommand(request.Email);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Request a password reset code")
        .WithDescription("Sends a password reset OTP to the email address if an account exists. Always returns 200 to avoid email enumeration.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class ResetPasswordEndpoint : IEndpoint
{
    internal sealed record Request(string Email, string Code, string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/reset-password", async (Request request, ISender sender) =>
        {
            var command = new ResetPasswordCommand(request.Email, request.Code, request.NewPassword);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Reset password using OTP code")
        .WithDescription("Sets a new password for the account after verifying the OTP code that was sent to the user's email.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.SendOtp;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class SendOtpEndpoint : IEndpoint
{
    internal sealed record Request(Guid UserId, string Purpose);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/send-otp", async (Request request, ISender sender) =>
        {
            var command = new SendOtpCommand(request.UserId, request.Purpose);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Send OTP to the user")
        .WithDescription("Generates and sends a one-time password (OTP) for the specified user and purpose (e.g., phone verification, email verification).")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

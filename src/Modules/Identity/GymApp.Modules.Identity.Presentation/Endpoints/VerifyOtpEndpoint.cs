using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class VerifyOtpEndpoint : IEndpoint
{
    internal sealed record Request(Guid UserId, string Code, string Purpose);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/verify-otp", async (Request request, ISender sender) =>
        {
            var command = new VerifyOtpCommand(request.UserId, request.Code, request.Purpose);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Verify an OTP code")
        .WithDescription("Validates a one-time password (OTP) for a user and purpose. Marks the associated verification step as complete on success.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }
}

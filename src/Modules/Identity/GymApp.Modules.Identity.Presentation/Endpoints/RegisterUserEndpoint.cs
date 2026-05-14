using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.RegisterUser;
using GymApp.Modules.Identity.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class RegisterUserEndpoint : IEndpoint
{
    internal sealed record Request(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Phone,
        string Role,
        bool DataProcessingConsent = false,
        bool MarketingConsent = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/register", async (Request request, ISender sender) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Role,
                request.DataProcessingConsent,
                request.MarketingConsent);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Register a new user")
        .WithDescription("Creates a new user account and returns JWT tokens. The account requires OTP verification before it is fully active.")
        .Produces<ApiResponse<TokenResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }
}

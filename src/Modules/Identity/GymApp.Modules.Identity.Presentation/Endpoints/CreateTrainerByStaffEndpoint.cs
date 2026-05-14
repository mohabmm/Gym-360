using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.CreateTrainerByStaff;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class CreateTrainerByStaffEndpoint : IEndpoint
{
    internal sealed record Request(
        string FirstName,
        string LastName,
        string Phone,
        string? Email);

    internal sealed record Response(Guid UserId, string InitialPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/users/staff-create-trainer", async (Request request, HttpContext http, ISender sender) =>
        {
            var gymClaim = http.User.FindFirstValue("gymId");
            if (!Guid.TryParse(gymClaim, out var gymId) || gymId == Guid.Empty)
                return ApiResults.Problem(new GymApp.Common.Domain.Error("Auth.GymContextMissing", "Staff JWT is missing a gym scope. Re-login or call /identity/select-gym."));

            var command = new CreateTrainerByStaffCommand(
                gymId,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Email);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(new Response(result.Value, "123456"))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Create a trainer user account (staff-driven)")
        .WithDescription("Creates a User with role=Trainer on behalf of a gym staff member. The trainer's initial password is hardcoded to `123456` and returned as `initialPassword` in the response — staff should display it once and instruct the trainer to change it on first login. If a user with the provided phone or email already exists, that user's Id is returned (and password reset to `123456`) so the caller can enroll them in a new gym. Does not return auth tokens.")
        .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("gym_staff");
    }
}

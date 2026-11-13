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

    /// <param name="InitialPassword">Null when the trainer already had an account (password unchanged).</param>
    internal sealed record Response(Guid UserId, string? InitialPassword);

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
                ? ApiResults.Created(new Response(result.Value.UserId, result.Value.InitialPassword))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Create a trainer user account (staff-driven)")
        .WithDescription("Creates a User with role=Trainer on behalf of a gym staff member. A new trainer's initial password is `12345678`, returned as `initialPassword` — staff should display it once and instruct the trainer to change it on first login. If an existing trainer has the provided phone or email, that user's Id is returned with `initialPassword: null` so the caller can enroll them in a new gym; their password is NOT changed. Does not return auth tokens.")
        .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("gym_staff");
    }
}

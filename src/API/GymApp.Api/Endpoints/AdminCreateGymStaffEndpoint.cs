using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.AddStaff;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Endpoints;

internal sealed class AdminCreateGymStaffEndpoint : IEndpoint
{
    internal sealed record Request(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? Phone,
        string? StaffSubRole);

    internal sealed record Response(Guid UserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/gyms/{gymId:guid}/staff/create", async (
            Guid gymId,
            Request request,
            ISender sender,
            CancellationToken ct) =>
        {
            // Step 1: create the User (or reuse) and link the gym membership.
            var createUser = new CreateGymStaffCommand(
                gymId,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone);

            var userResult = await sender.Send(createUser, ct);
            if (userResult.IsFailure)
                return ApiResults.Problem(userResult.Error);

            // Step 2: assign the gym-side StaffRole row (drives permissions).
            var role = string.IsNullOrWhiteSpace(request.StaffSubRole) ? "Receptionist" : request.StaffSubRole;
            var addStaff = new AddStaffCommand(gymId, userResult.Value, role);

            var staffResult = await sender.Send(addStaff, ct);
            if (staffResult.IsFailure)
                return ApiResults.Problem(staffResult.Error);

            return ApiResults.Created(new Response(userResult.Value), $"api/v1/gyms/{gymId}/staff");
        })
        .WithTags("Admin", "Gyms")
        .WithSummary("Create a gym staff user and assign them to the gym")
        .WithDescription("Provisions a new User (role=GymStaff, activated), creates a gym membership, and assigns a StaffRole at the gym (Owner/Manager/Receptionist). If the email is already in use, the existing user is reused only if they have no active gym membership. Requires platform_admin.")
        .Produces<ApiResponse<Response>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");
    }
}

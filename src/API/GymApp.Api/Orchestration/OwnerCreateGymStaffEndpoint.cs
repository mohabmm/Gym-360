using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.AddStaff;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Orchestration;

internal sealed class OwnerCreateGymStaffEndpoint : IEndpoint
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
        app.MapPost("api/v1/gyms/{gymId:guid}/staff/create", async (
            Guid gymId,
            Request request,
            ISender sender,
            CancellationToken ct) =>
        {
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

            var role = string.IsNullOrWhiteSpace(request.StaffSubRole) ? "Receptionist" : request.StaffSubRole;
            var addStaff = new AddStaffCommand(gymId, userResult.Value, role);

            var staffResult = await sender.Send(addStaff, ct);
            if (staffResult.IsFailure)
                return ApiResults.Problem(staffResult.Error);

            return ApiResults.Created(new Response(userResult.Value), $"api/v1/gyms/{gymId}/staff");
        })
        .WithTags("Gyms")
        .WithSummary("Owner-callable: create a gym staff user and assign them to the gym")
        .WithDescription("Mirrors POST /api/v1/admin/gyms/{gymId}/staff/create but is callable by the gym owner (gym_owner_or_admin). Provisions a new User (role=GymStaff, activated), creates the gym membership, and assigns a StaffRole at the gym.")
        .Produces<ApiResponse<Response>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("gym_owner_or_admin");
    }
}

using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Commands.AddMember;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Identity.Application.Commands.CreateMemberByStaff;
using GymApp.Modules.Identity.Application.Commands.LinkMemberToGym;
using GymApp.Modules.Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Endpoints;

internal sealed class StaffCreateMemberEndpoint : IEndpoint
{
    internal sealed record Request(
        string FirstName,
        string LastName,
        string Phone,
        string? Email,
        string? Section);

    internal sealed record Response(Guid UserId, string InitialPassword, string MemberNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/users/staff-create", async (
            Request request,
            HttpContext http,
            ISender sender,
            IGymMemberDirectory directory,
            IGymsUnitOfWork gymsUow,
            CancellationToken ct) =>
        {
            var gymClaim = http.User.FindFirstValue("gymId");
            if (!Guid.TryParse(gymClaim, out var gymId) || gymId == Guid.Empty)
                return ApiResults.Problem(new Error("Auth.GymContextMissing", "Staff JWT is missing a gym scope. Re-login or call /identity/select-gym."));

            var createCmd = new CreateMemberByStaffCommand(
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Email);

            var createResult = await sender.Send(createCmd, ct);
            if (createResult.IsFailure)
                return ApiResults.Problem(createResult.Error);

            var userId = createResult.Value;

            // AddMemberCommand is the authoritative path: it (a) calls Gym.AddMember
            // which updates the Gym.MemberIds JSON column the membership checks read,
            // (b) registers a gym_members row via IGymMemberDirectory and assigns a
            // MemberNumber, and (c) publishes MemberAddedToGymIntegrationEvent. Skipping
            // it (calling directory.RegisterAsync alone) leaves /me/* endpoints returning
            // MEMBER_ROLE_REQUIRED because IsActiveMemberAsync reads MemberIds.
            var addCmd = new AddMemberCommand(gymId, userId, request.Section, MemberNumber: null);
            var addResult = await sender.Send(addCmd, ct);
            if (addResult.IsFailure)
                return ApiResults.Problem(addResult.Error);

            // Read the assigned MemberNumber back so the response shape stays stable
            // (clients show this number on the trainee profile / QR check-in screen).
            var memberNumber = await directory.GetMemberNumberAsync(gymId, userId, ct) ?? string.Empty;

            var linkCmd = new LinkMemberToGymCommand(userId, gymId, UserRole.Trainee, request.Section, memberNumber);
            var linkResult = await sender.Send(linkCmd, ct);
            if (linkResult.IsFailure)
                return ApiResults.Problem(linkResult.Error);

            return ApiResults.Created(new Response(userId, "123456", memberNumber));
        })
        .WithTags("Identity")
        .WithSummary("Create a member user account (staff-driven) and link to gym")
        .WithDescription("Creates a User with role=Trainee on behalf of a gym staff member, registers a unique MemberNumber for the gym, and links the user to the staff's gym. Initial password is `123456` (returned as `initialPassword`). Reuses an existing trainee with matching phone/email and resets their password. Requires staff JWT with a gym claim.")
        .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("gym_staff");
    }
}

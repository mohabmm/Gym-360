using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Commands.AddMember;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Identity.Application.Commands.LinkMemberToGym;
using GymApp.Modules.Identity.Application.Queries.LookupTraineeByPhone;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Orchestration;

/// <summary>
/// Re-links an EXISTING trainee to the caller's gym ("Add existing member"). This is the
/// sanctioned path for adding a returning member whose phone/email already exists — it
/// reuses the account WITHOUT creating a duplicate or resetting their password, then runs
/// the same AddMember + LinkMemberToGym tail as staff-create. The silent reuse that used
/// to live inside staff-create now lives here, behind an explicit staff confirmation.
/// </summary>
internal sealed class StaffLinkExistingMemberEndpoint : IEndpoint
{
    internal sealed record Request(string Phone, string? Section);

    internal sealed record Response(Guid UserId, string MemberNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/users/staff-link-existing", async (
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

            var lookup = await sender.Send(new LookupTraineeByPhoneQuery(request.Phone), ct);
            if (lookup.IsFailure)
                return ApiResults.Problem(lookup.Error);

            var userId = lookup.Value.UserId;

            // One-Gym pre-check: if the trainee is ACTIVE at a different gym, reject here —
            // before AddMember mutates this gym's MemberIds — so we never leave a half-linked
            // member (in MemberIds but with no Identity membership) when LinkMemberToGym would
            // fail the One-Gym rule anyway. (Active at THIS gym is fine — re-link is idempotent.)
            var activeGymId = lookup.Value.ActiveGymId;
            if (activeGymId is not null && activeGymId != gymId)
                return ApiResults.Problem(UserErrors.AlreadyActiveInAnotherGym);

            // Same authoritative tail as StaffCreateMemberEndpoint: AddMemberCommand updates
            // Gym.MemberIds (read by membership checks), registers a gym_members row + assigns
            // a MemberNumber, and publishes MemberAddedToGymIntegrationEvent.
            var addResult = await sender.Send(new AddMemberCommand(gymId, userId, request.Section, MemberNumber: null), ct);
            if (addResult.IsFailure)
                return ApiResults.Problem(addResult.Error);

            var memberNumber = await directory.GetMemberNumberAsync(gymId, userId, ct) ?? string.Empty;

            var linkResult = await sender.Send(
                new LinkMemberToGymCommand(userId, gymId, UserRole.Trainee, request.Section, memberNumber), ct);
            if (linkResult.IsFailure)
                return ApiResults.Problem(linkResult.Error);

            return ApiResults.Created(new Response(userId, memberNumber));
        })
        .WithTags("Identity")
        .WithSummary("Link an existing trainee to the staff's gym (returning member)")
        .WithDescription("Re-links an existing trainee (resolved by phone) to the caller's gym without creating a duplicate or resetting their password. Use after staff-lookup confirms identity. Returns 409 if the trainee is already active at another gym (One-Gym rule). Requires staff JWT with a gym claim.")
        .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("gym_staff");
    }
}

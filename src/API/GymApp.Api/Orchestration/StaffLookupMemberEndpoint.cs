using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Identity.Application.Queries.LookupTraineeByPhone;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Orchestration;

/// <summary>
/// Staff-facing preview: given a phone number, returns the existing trainee's identity
/// (if any) plus whether they already belong to the caller's gym. The Add Member UI
/// calls this when staff-create reports a duplicate, so reception can confirm "this is
/// the same person" before re-linking via <see cref="StaffLinkExistingMemberEndpoint"/>.
/// </summary>
internal sealed class StaffLookupMemberEndpoint : IEndpoint
{
    internal sealed record Response(
        Guid UserId,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool IsAlreadyMemberOfThisGym);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/identity/users/staff-lookup", HandleAsync)
        .WithTags("Identity")
        .WithSummary("Look up an existing trainee by phone (staff-driven)")
        .WithDescription("Resolves an existing trainee by phone number so staff can confirm a returning member before re-linking. Returns 404 when no trainee matches. Requires staff JWT with a gym claim.")
        .Produces<ApiResponse<Response>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("gym_staff");
    }

    private static async Task<IResult> HandleAsync(
        string phone,
        HttpContext http,
        ISender sender,
        IGymMembershipChecker membershipChecker)
    {
        var ct = http.RequestAborted;
        if (!StaffGymScope.TryGetGymId(http.User, out var gymId))
            return ApiResults.Problem(StaffGymScope.Missing);

        var lookup = await sender.Send(new LookupTraineeByPhoneQuery(phone), ct);
        if (lookup.IsFailure)
            return ApiResults.Problem(lookup.Error);

        var t = lookup.Value;
        // Authoritative "currently a member here" signal reads Gym.MemberIds — NOT the
        // directory MemberNumber, which survives RemoveMember and would wrongly report a
        // removed (returning) member as still active, blocking the re-link they need.
        var alreadyMember = await membershipChecker.IsActiveMemberAsync(gymId, t.UserId, ct);

        return ApiResults.Ok(new Response(
            t.UserId, t.FirstName, t.LastName, t.Email, t.Phone, alreadyMember));
    }
}

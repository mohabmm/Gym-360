using GymApp.Common.Domain;
using GymApp.Common.Presentation;
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
        app.MapPost("api/v1/identity/users/staff-link-existing", HandleAsync)
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

    private static async Task<IResult> HandleAsync(
        Request request,
        HttpContext http,
        ISender sender,
        IGymMemberDirectory directory)
    {
        var ct = http.RequestAborted;
        if (!StaffGymScope.TryGetGymId(http.User, out var gymId))
            return ApiResults.Problem(StaffGymScope.Missing);

        var lookup = await sender.Send(new LookupTraineeByPhoneQuery(request.Phone), ct);
        if (lookup.IsFailure)
            return ApiResults.Problem(lookup.Error);

        // One-Gym pre-check: if the trainee is ACTIVE at a different gym, reject here —
        // before AddMember mutates this gym's MemberIds — so we never leave a half-linked
        // member (in MemberIds but with no Identity membership) when LinkMemberToGym would
        // fail the One-Gym rule anyway. (Active at THIS gym is fine — re-link is idempotent.)
        if (IsActiveElsewhere(lookup.Value.ActiveGymId, gymId))
            return ApiResults.Problem(UserErrors.AlreadyActiveInAnotherGym);

        var linkResult = await AddAndLinkAsync(new LinkTarget(gymId, lookup.Value.UserId, request.Section), sender, directory, ct);
        return linkResult.IsFailure
            ? ApiResults.Problem(linkResult.Error)
            : ApiResults.Created(linkResult.Value);
    }

    private static bool IsActiveElsewhere(Guid? activeGymId, Guid gymId) =>
        activeGymId is not null && activeGymId != gymId;

    private sealed record LinkTarget(Guid GymId, Guid UserId, string? Section);

    private static async Task<Result<Response>> AddAndLinkAsync(
        LinkTarget target,
        ISender sender,
        IGymMemberDirectory directory,
        CancellationToken ct)
    {
        // Same authoritative tail as StaffCreateMemberEndpoint: AddMemberCommand updates
        // Gym.MemberIds (read by membership checks), registers a gym_members row + assigns
        // a MemberNumber, and publishes MemberAddedToGymIntegrationEvent.
        var addResult = await sender.Send(new AddMemberCommand(target.GymId, target.UserId, target.Section, MemberNumber: null), ct);
        if (addResult.IsFailure)
            return Result.Failure<Response>(addResult.Error);

        var memberNumber = await directory.GetMemberNumberAsync(target.GymId, target.UserId, ct) ?? string.Empty;

        var linkResult = await sender.Send(
            new LinkMemberToGymCommand(target.UserId, target.GymId, UserRole.Trainee, target.Section, memberNumber), ct);
        if (linkResult.IsFailure)
            return Result.Failure<Response>(linkResult.Error);

        return Result.Success(new Response(target.UserId, memberNumber));
    }
}

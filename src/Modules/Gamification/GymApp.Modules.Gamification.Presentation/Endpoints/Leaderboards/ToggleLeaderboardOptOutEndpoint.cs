using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.Commands.ToggleLeaderboardOptOut;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Leaderboards;

internal sealed class ToggleLeaderboardOptOutEndpoint : IEndpoint
{
    // Body carries only GymId; the user (caller) is derived from the JWT 'sub' claim
    // so a member can only opt themselves out, never another user.
    internal sealed record ToggleLeaderboardOptOutRequest(Guid GymId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gamification/leaderboards/opt-out", async (
            ToggleLeaderboardOptOutRequest request,
            HttpContext http,
            ISender sender) =>
        {
            var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? http.User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var command = new ToggleLeaderboardOptOutCommand(userId, request.GymId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Toggle leaderboard opt-out for a member")
        .WithDescription("Toggles the member's leaderboard visibility preference; opted-out members are hidden from public rankings.")
        .Produces(204)
        .WithTags("Gamification")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}

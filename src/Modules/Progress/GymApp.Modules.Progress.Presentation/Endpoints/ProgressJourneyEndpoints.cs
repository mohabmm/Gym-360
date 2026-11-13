using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Progress.Application.Commands.SetProgressGoal;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Queries.GetProgressJourney;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

/// <summary>
/// pt.progress_measurements: measurements since day one + the member's goal.
/// Member: <c>GET /progress/journey</c>, <c>PUT /progress/goal</c>.
/// Coach (own clients only): <c>GET /progress/clients/{clientId}/journey</c>,
/// <c>PUT /progress/clients/{clientId}/goal</c>. The caller always comes from the JWT.
/// </summary>
internal sealed class ProgressJourneyEndpoints : IEndpoint
{
    internal sealed record GoalRequest(string Goal, decimal? TargetWeight);

    private sealed record Caller(Guid UserId, Guid GymId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/progress/journey", (Guid? gymId, HttpContext http, ISender sender) =>
                WithCaller(http, gymId, c => sender.Send(new GetProgressJourneyQuery(c.UserId, c.GymId, null)), v => ApiResults.Ok(v)))
            .RequireAuthorization()
            .RequireFeature(FeatureKeys.PtProgressMeasurements)
            .WithTags("Progress")
            .WithSummary("My progress since day one")
            .WithDescription("All of the member's body-metric entries (oldest first) and their goal.")
            .Produces<ApiResponse<ProgressJourneyResponse>>(StatusCodes.Status200OK);

        app.MapGet("api/v1/progress/clients/{clientId:guid}/journey", (Guid clientId, Guid? gymId, HttpContext http, ISender sender) =>
                WithCaller(http, gymId, c => sender.Send(new GetProgressJourneyQuery(clientId, c.GymId, c.UserId)), v => ApiResults.Ok(v)))
            .RequireAuthorization()
            .RequireFeature(FeatureKeys.PtProgressMeasurements)
            .WithTags("Progress")
            .WithSummary("A client's progress since day one (coach)")
            .WithDescription("Only for clients with an active package with the calling coach; otherwise 403.")
            .Produces<ApiResponse<ProgressJourneyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        app.MapPut("api/v1/progress/goal", (Guid? gymId, GoalRequest request, HttpContext http, ISender sender) =>
                WithCaller(http, gymId, c => sender.Send(GoalCommand(c.UserId, c.GymId, null, request)), v => ApiResults.Ok(v)))
            .RequireAuthorization()
            .RequireFeature(FeatureKeys.PtProgressMeasurements)
            .WithTags("Progress")
            .WithSummary("Set my goal")
            .Produces<ApiResponse<ProgressGoalDto>>(StatusCodes.Status200OK);

        app.MapPut("api/v1/progress/clients/{clientId:guid}/goal", (Guid clientId, Guid? gymId, GoalRequest request, HttpContext http, ISender sender) =>
                WithCaller(http, gymId, c => sender.Send(GoalCommand(clientId, c.GymId, c.UserId, request)), v => ApiResults.Ok(v)))
            .RequireAuthorization()
            .RequireFeature(FeatureKeys.PtProgressMeasurements)
            .WithTags("Progress")
            .WithSummary("Set a client's goal (coach)")
            .Produces<ApiResponse<ProgressGoalDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);
    }

    private static SetProgressGoalCommand GoalCommand(Guid memberId, Guid gymId, Guid? trainerId, GoalRequest request) =>
        new(memberId, gymId, trainerId, request.Goal, request.TargetWeight);

    /// <summary>Resolves the caller (JWT user + gym), runs the request and maps the result.</summary>
    private static async Task<IResult> WithCaller<T>(
        HttpContext http,
        Guid? queryGymId,
        Func<Caller, Task<Result<T>>> send,
        Func<T, IResult> onSuccess)
    {
        if (!CallerContext.TryGetUserId(http, out var userId))
            return Results.Unauthorized();

        var gymId = ResolveGymId(http, queryGymId);
        if (gymId is null)
            return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "A gym is required (token gym, X-Gym-Id or gymId).", 400);

        var result = await send(new Caller(userId, gymId.Value));
        return result.IsSuccess ? onSuccess(result.Value) : ApiResults.Problem(result.Error);
    }

    // The token's gym wins; mobile apps without one send X-Gym-Id or ?gymId like the other progress routes.
    private static Guid? ResolveGymId(HttpContext http, Guid? queryGymId)
    {
        if (CallerContext.TryGetGymId(http, out var fromToken))
            return fromToken;
        if (Guid.TryParse(http.Request.Headers["X-Gym-Id"].ToString(), out var fromHeader))
            return fromHeader;
        return queryGymId is { } q && q != Guid.Empty ? q : null;
    }
}

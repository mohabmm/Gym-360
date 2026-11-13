using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Scheduling.Application.Commands.RespondToSession;
using MediatR;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

/// <summary>pt.dual_session_logging — trainee confirms a logged session or says "not me".</summary>
internal sealed class RespondToSessionEndpoint : IEndpoint
{
    internal sealed record DisputeRequest(string? Reason);

    /// <summary>The trainee's answer for one session.</summary>
    private sealed record SessionAnswer(Guid SessionId, bool Confirmed, string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/member-confirm", (
                Guid sessionId, HttpContext httpContext, ISender sender) =>
            RespondAsync(new SessionAnswer(sessionId, Confirmed: true, Reason: null), httpContext, sender))
        .RequireAuthorization("trainee")
        .RequireFeature(FeatureKeys.PtDualSessionLogging)
        .WithTags("Scheduling")
        .WithSummary("Trainee confirms a logged session")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/member-dispute", (
                Guid sessionId, DisputeRequest? request, HttpContext httpContext, ISender sender) =>
            RespondAsync(new SessionAnswer(sessionId, Confirmed: false, request?.Reason), httpContext, sender))
        .RequireAuthorization("trainee")
        .RequireFeature(FeatureKeys.PtDualSessionLogging)
        .WithTags("Scheduling")
        .WithSummary("Trainee says a logged session did not happen")
        .WithDescription("Marks the session Disputed and alerts the desk, who can reverse it (restoring the package session).")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RespondAsync(SessionAnswer answer, HttpContext httpContext, ISender sender)
    {
        if (!UserContext.TryGetUserId(httpContext, out var memberId))
            return Results.Unauthorized();

        var result = await sender.Send(new RespondToSessionCommand(answer.SessionId, memberId, answer.Confirmed, answer.Reason));
        return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result.Error);
    }
}

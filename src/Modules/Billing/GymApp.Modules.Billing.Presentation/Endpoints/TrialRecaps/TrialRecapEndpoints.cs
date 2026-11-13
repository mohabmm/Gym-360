using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Billing.Application.Commands.TrialRecaps;
using GymApp.Modules.Billing.Application.Queries.TrialRecaps;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.TrialRecaps;

/// <summary>
/// pt.trial_recap:
/// <c>POST /api/v1/billing/trials/{packageId}/recap</c> (the trial's coach) and
/// <c>GET /api/v1/billing/trials/my-recap</c> (the trainee).
/// </summary>
internal sealed class TrialRecapEndpoints : IEndpoint
{
    internal sealed record SendRecapRequest(string Note, int RecommendedSessions);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/trials/{packageId:guid}/recap", async (
            Guid packageId,
            SendRecapRequest request,
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var gymId = GymContext.Resolve(httpContext, headerGymId);
            if (gymId is null)
                return GymContext.Required();

            var result = await sender.Send(new SendTrialRecapCommand(
                gymId.Value, packageId, trainerId, request.Note, request.RecommendedSessions));
            return result.IsSuccess ? ApiResults.Ok(new { packageId }) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("trainer")
        .RequireFeature(FeatureKeys.PtTrialRecap)
        .WithTags("Billing")
        .WithSummary("Coach sends the trainee a post-trial plan and a recommended package size (no price)")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        app.MapGet("api/v1/billing/trials/my-recap", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var memberId))
                return Results.Unauthorized();

            var gymId = GymContext.Resolve(httpContext, headerGymId);
            if (gymId is null)
                return GymContext.Required();

            var result = await sender.Send(new GetMyTrialRecapQuery(gymId.Value, memberId));
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("trainee")
        .RequireFeature(FeatureKeys.PtTrialRecap)
        .WithTags("Billing")
        .WithSummary("The trainee's latest post-trial plan from their coach (404 until one is sent)")
        .Produces<ApiResponse<TrialRecapDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }
}

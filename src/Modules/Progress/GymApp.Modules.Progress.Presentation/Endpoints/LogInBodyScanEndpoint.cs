using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Progress.Application.Commands.LogInBodyScan;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

/// <summary>
/// <c>POST /progress/body-metrics/inbody</c>: the member saves the values read from their InBody
/// result sheet (OCR runs on the phone; only the confirmed numbers are sent). The member comes
/// from the JWT and the gym from <c>X-Gym-Id</c>. 201 for a new entry, 200 when the scan
/// updated the entry already logged for that day.
/// </summary>
internal sealed class LogInBodyScanEndpoint : IEndpoint
{
    internal sealed record Request(
        DateOnly Date,
        decimal? WeightKg,
        decimal? HeightCm,
        decimal? BodyFatPercentage,
        decimal? SkeletalMuscleMassKg,
        decimal? BodyWaterKg,
        string? Notes);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/progress/body-metrics/inbody", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            Request request,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var result = await sender.Send(ToCommand(userId, gymId.Value, request));

            if (result.IsFailure)
                return ApiResults.Problem(result.Error);

            return result.Value.Created
                ? ApiResults.Created(result.Value)
                : ApiResults.Ok(result.Value);
        })
        .RequireAuthorization()
        .RequireFeature(FeatureKeys.ProgressInBody)
        .WithTags("Progress")
        .WithSummary("Save an InBody scan")
        .WithDescription("Saves weight, height, body fat %, skeletal muscle mass and body water read from the member's InBody sheet. One entry per day: a scan on a day that already has one updates it.")
        .Produces<ApiResponse<LogInBodyScanResult>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<LogInBodyScanResult>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);
    }

    internal static LogInBodyScanCommand ToCommand(Guid userId, Guid gymId, Request request) =>
        new(
            userId,
            gymId,
            request.Date,
            new InBodyScanValues(
                request.WeightKg,
                request.HeightCm,
                request.BodyFatPercentage,
                request.SkeletalMuscleMassKg,
                request.BodyWaterKg),
            request.Notes);
}

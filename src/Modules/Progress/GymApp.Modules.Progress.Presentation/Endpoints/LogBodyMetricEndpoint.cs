using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.Commands.LogBodyMetric;
using GymApp.Modules.Progress.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class LogBodyMetricEndpoint : IEndpoint
{
    // Identity (UserId, LoggedBy) is derived from the JWT; gym scope from the X-Gym-Id header.
    // Previously these came from the request body, which let an authenticated user log metrics
    // for any other user. Clients only send the actual metric data.
    internal sealed record Request(
        DateOnly Date,
        decimal? Weight,
        string? WeightUnit,
        decimal? BodyFatPercentage,
        BodyMeasurementsDto? Measurements,
        string? Source,
        string? Notes);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/progress/body-metrics", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            Request request,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var command = new LogBodyMetricCommand(
                userId,
                gymId.Value,
                request.Date,
                request.Weight,
                request.WeightUnit,
                request.BodyFatPercentage,
                request.Measurements,
                request.Source ?? "SelfReported",
                userId,
                request.Notes);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Log body metric entry")
        .WithDescription("Records a body metric snapshot (weight, body fat percentage, and body measurements) for the authenticated member at the gym specified by X-Gym-Id.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

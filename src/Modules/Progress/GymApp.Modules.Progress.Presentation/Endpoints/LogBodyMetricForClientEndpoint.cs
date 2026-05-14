using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.Commands.LogBodyMetricForClient;
using GymApp.Modules.Progress.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class LogBodyMetricForClientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/progress/body-metrics/client", async (LogBodyMetricForClientRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new LogBodyMetricForClientCommand(
                trainerId,
                request.ClientId,
                request.GymId,
                request.Date,
                request.Weight,
                request.WeightUnit,
                request.BodyFatPercentage,
                request.Measurements,
                request.Notes);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Log body metric for a client")
        .WithDescription("Allows a trainer to record body metric measurements on behalf of one of their clients.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record LogBodyMetricForClientRequest(
    Guid ClientId,
    Guid GymId,
    DateOnly Date,
    decimal? Weight,
    string? WeightUnit,
    decimal? BodyFatPercentage,
    BodyMeasurementsDto? Measurements,
    string? Notes);

using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.LogWater;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class LogWaterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/nutrition/water-log", async (LogWaterRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            // Prefer JWT-bound gymId so a trainee can't write to a foreign tenant by spoofing body.
            // Fall back to body for back-compat with older clients during rollout.
            var gymId = CallerContext.TryGetGymId(httpContext, out var jwtGym) ? jwtGym : request.GymId;
            if (IsCrossGymWrite(request.GymId, jwtGym))
                return ApiResults.Problem(new GymApp.Common.Domain.Error(
                    "AUTH.CROSS_GYM_FORBIDDEN", "Body gymId does not match the caller's tenant."));

            var command = new LogWaterCommand(
                userId,
                gymId,
                request.Date,
                request.AmountMl);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Log water intake")
        .WithDescription("Logs a water intake entry for a member on a specific date in millilitres.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }

    // Both gym ids are known and disagree: the body targets a tenant other than the caller's.
    private static bool IsCrossGymWrite(Guid bodyGymId, Guid jwtGymId)
    {
        if (bodyGymId == Guid.Empty || jwtGymId == Guid.Empty)
            return false;

        return bodyGymId != jwtGymId;
    }
}

internal sealed record LogWaterRequest(
    Guid GymId,
    DateOnly Date,
    int AmountMl);

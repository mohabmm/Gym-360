using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Commands.SetWaterTarget;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

internal sealed class SetWaterTargetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/nutrition/water-log/target", async (SetWaterTargetRequest request, HttpContext http, ISender sender) =>
        {
            // UserId derived from JWT — body cannot set another user's target.
            var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var command = new SetWaterTargetCommand(
                userId,
                request.GymId,
                request.Date,
                request.TargetMl);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Nutrition")
        .WithSummary("Set daily water target")
        .WithDescription("Sets or updates the daily water intake target (in millilitres) for a member on a specific date.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

// UserId removed from body — derived from JWT to prevent setting another user's target.
internal sealed record SetWaterTargetRequest(
    Guid GymId,
    DateOnly Date,
    int TargetMl);

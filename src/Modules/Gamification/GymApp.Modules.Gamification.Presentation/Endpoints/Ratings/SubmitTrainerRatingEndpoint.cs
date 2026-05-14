using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.Commands.SubmitTrainerRating;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Ratings;

internal sealed class SubmitTrainerRatingEndpoint : IEndpoint
{
    internal sealed record SubmitTrainerRatingRequest(
        Guid GymId,
        Guid TrainerId,
        Guid SessionId,
        int Stars,
        string? Comment);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gamification/trainer-ratings", async (
            SubmitTrainerRatingRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var clientId))
                return Results.Unauthorized();

            var command = new SubmitTrainerRatingCommand(
                request.GymId,
                request.TrainerId,
                clientId,
                request.SessionId,
                request.Stars,
                request.Comment);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/gamification/trainer-ratings/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Submit a trainer rating")
        .WithDescription("Allows a client to submit a star rating and optional comment for their trainer after a completed session.")
        .WithTags("Gamification")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.BookSession;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class BookSessionEndpoint : IEndpoint
{
    // MemberId and GymId come from the authenticated context (JWT + X-Gym-Id header) so a
    // trainee cannot book a session on someone else's behalf by tampering with the request body.
    internal sealed record Request(
        Guid TrainerId,
        DateOnly Date,
        TimeOnly Time,
        int DurationMinutes,
        string SessionType,
        string? Focus);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            Request request,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var memberId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var command = new BookSessionCommand(
                gymId.Value,
                request.TrainerId,
                memberId,
                request.Date,
                request.Time,
                request.DurationMinutes,
                request.SessionType,
                request.Focus);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Book a training session")
        .WithDescription("The authenticated trainee books a one-on-one training session with a trainer at the gym specified by X-Gym-Id. Requires an active, non-expired session package with remaining sessions.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

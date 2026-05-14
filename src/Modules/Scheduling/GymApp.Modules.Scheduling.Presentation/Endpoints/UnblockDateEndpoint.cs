using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.UnblockDate;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class UnblockDateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/availability/unblock-date", async (UnblockDateRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new UnblockDateCommand(trainerId, request.GymId, request.Date);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Unblock a previously blocked date")
        .WithDescription("Removes a date from the trainer's blocked-dates list, restoring it as available for booking.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record UnblockDateRequest(Guid GymId, DateOnly Date);

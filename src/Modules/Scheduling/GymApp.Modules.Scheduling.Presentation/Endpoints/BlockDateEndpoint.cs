using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.BlockDate;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class BlockDateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/availability/block-date", async (BlockDateRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new BlockDateCommand(
                trainerId,
                request.GymId,
                request.Date,
                request.Reason,
                request.AutoNotifyClients);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Block a date in trainer availability")
        .WithDescription("Marks a specific date as unavailable for a trainer, optionally sending automatic notifications to affected clients.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record BlockDateRequest(
    Guid GymId,
    DateOnly Date,
    string? Reason,
    bool AutoNotifyClients);

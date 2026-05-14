using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetMyWorkoutAssignments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetMyWorkoutAssignmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/workout-assignments", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var query = new GetMyWorkoutAssignmentsQuery(userId, gymId.Value);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Get workout assignments for the authenticated trainee")
        .WithDescription("Returns all workout and program assignments for the authenticated user at the specified gym.")
        .Produces<ApiResponse<List<WorkoutAssignmentDto>>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}

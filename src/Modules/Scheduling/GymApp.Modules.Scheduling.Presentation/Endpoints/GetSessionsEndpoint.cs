using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetSessions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class GetSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/sessions", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            [FromQuery(Name = "gymId")] Guid? queryGymId,
            Guid? trainerId,
            Guid? memberId,
            DateOnly? from,
            DateOnly? to,
            string? status,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var resolved = headerGymId ?? queryGymId;
            if (resolved is null || resolved == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var query = new GetSessionsQuery(resolved.Value, trainerId, memberId, from, to, status, page, pageSize);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Get sessions list")
        .WithDescription("Returns a paginated list of sessions for a gym, filterable by trainer, member, date range, and status.")
        .Produces<ApiResponse<PagedResponse<SessionDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

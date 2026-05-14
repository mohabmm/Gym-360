using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.SessionPackages;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.SessionPackages;

internal sealed class GetMyClientPackagesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/packages/my-clients", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymIdHeader,
            [FromQuery] Guid? gymId,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var effectiveGymId = gymId ?? gymIdHeader ?? UserContext.TryGetGymId(httpContext);
            if (effectiveGymId is null || effectiveGymId == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var query = new GetMyClientPackagesQuery(trainerId, effectiveGymId.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get trainer's client session packages")
        .WithDescription("Returns all session packages assigned to the authenticated trainer's clients at the selected gym. TrainerId is taken from the JWT.")
        .WithTags("Billing")
        .Produces<ApiResponse<List<SessionPackageDto>>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}

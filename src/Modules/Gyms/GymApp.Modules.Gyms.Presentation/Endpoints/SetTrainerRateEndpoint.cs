using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.SetTrainerRate;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class SetTrainerRateEndpoint : IEndpoint
{
    // SetByUserId derived from caller's JWT — body cannot impersonate another setter for audit logs.
    internal sealed record Request(decimal RatePerSession);

    internal sealed record TrainerRoute(Guid GymId, Guid TrainerId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/rate",
            async ([AsParameters] TrainerRoute route, Request request, HttpContext http, ISender sender) =>
            {
                var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
                if (!Guid.TryParse(sub, out var setByUserId) || setByUserId == Guid.Empty)
                    return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

                var command = new SetTrainerRateCommand(
                    route.GymId,
                    route.TrainerId,
                    request.RatePerSession,
                    setByUserId);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Ok(new ApiResponse<object>(null))
                    : ApiResults.Problem(result.Error);
            })
        .WithTags("Gyms")
        .WithSummary("Set trainer session rate")
        .WithDescription("Sets or updates the flat per-session EGP rate for a trainer at a specific gym. Requires gym_owner or gym_staff role.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}

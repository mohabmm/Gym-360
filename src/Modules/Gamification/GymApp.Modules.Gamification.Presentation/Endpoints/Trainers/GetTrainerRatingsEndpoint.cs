using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetTrainerRatings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class GetTrainerRatingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/trainers/{trainerId:guid}/ratings", async (
            Guid trainerId,
            Guid? gymId,
            DateOnly? from,
            DateOnly? to,
            ITenantContext tenantContext,
            ISender sender) =>
        {
            var resolvedGymId = gymId ?? tenantContext.GymId;
            if (resolvedGymId is null || resolvedGymId == Guid.Empty)
            {
                return ApiResults.Problem(new Error("Gym.GymIdRequired", "gymId must be provided as a query parameter or via an authenticated gym context."));
            }

            var query = new GetTrainerRatingsQuery(resolvedGymId.Value, trainerId, from, to);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get ratings for a trainer")
        .WithDescription("Returns all ratings submitted for a specific trainer in a gym, with optional date range filtering.")
        .WithTags("Gamification")
        .Produces<ApiResponse<List<TrainerRatingDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}

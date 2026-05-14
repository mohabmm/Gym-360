using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetTrainerPublicProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class GetTrainerPublicProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/trainers/{trainerId:guid}/public-profile", async (
            Guid trainerId,
            Guid gymId,
            ISender sender) =>
        {
            var query = new GetTrainerPublicProfileQuery(trainerId, gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get trainer public profile")
        .WithDescription("Returns a trainer's public-facing profile including display rating, coach-of-the-month count, and approved testimonials.")
        .WithTags("Gamification")
        .Produces<ApiResponse<TrainerPublicProfileResponse>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}

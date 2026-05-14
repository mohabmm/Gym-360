using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetTrainerMonthlyScores;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class GetTrainerMonthlyScoresEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/trainer-monthly-scores", async (
            Guid gymId,
            int month,
            int year,
            ISender sender) =>
        {
            var query = new GetTrainerMonthlyScoresQuery(gymId, month, year);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get trainer monthly scores")
        .WithDescription("Returns aggregated monthly performance scores for all trainers in a gym for the specified month and year.")
        .WithTags("Gamification")
        .Produces<ApiResponse<List<TrainerMonthlyScoreDto>>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}

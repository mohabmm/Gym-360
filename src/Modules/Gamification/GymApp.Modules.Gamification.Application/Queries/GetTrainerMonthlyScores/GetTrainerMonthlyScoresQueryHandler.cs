using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Repositories;

namespace GymApp.Modules.Gamification.Application.Queries.GetTrainerMonthlyScores;

internal sealed class GetTrainerMonthlyScoresQueryHandler
    : IQueryHandler<GetTrainerMonthlyScoresQuery, List<TrainerMonthlyScoreDto>>
{
    private readonly ITrainerMonthlyScoreRepository _scoreRepository;

    public GetTrainerMonthlyScoresQueryHandler(ITrainerMonthlyScoreRepository scoreRepository)
    {
        _scoreRepository = scoreRepository;
    }

    public async Task<Result<List<TrainerMonthlyScoreDto>>> Handle(
        GetTrainerMonthlyScoresQuery request,
        CancellationToken cancellationToken)
    {
        var scores = await _scoreRepository.GetByGymAsync(
            request.GymId, request.Month, request.Year, cancellationToken);

        var dtos = scores
            .OrderByDescending(s => s.NormalizedScore)
            .Select(s => new TrainerMonthlyScoreDto(
                s.TrainerId,
                s.Month,
                s.Year,
                s.AggregatedScore,
                s.TotalRatings,
                s.UniqueClients,
                s.SessionCount,
                s.NormalizedScore,
                s.IsCoachOfTheMonth,
                s.CalculatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}

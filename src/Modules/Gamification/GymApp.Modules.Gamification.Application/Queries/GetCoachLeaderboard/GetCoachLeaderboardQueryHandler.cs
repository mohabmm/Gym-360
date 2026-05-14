using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Repositories;

namespace GymApp.Modules.Gamification.Application.Queries.GetCoachLeaderboard;

internal sealed class GetCoachLeaderboardQueryHandler
    : IQueryHandler<GetCoachLeaderboardQuery, List<CoachLeaderboardEntryDto>>
{
    private readonly ITrainerMonthlyScoreRepository _scoreRepository;

    public GetCoachLeaderboardQueryHandler(ITrainerMonthlyScoreRepository scoreRepository)
    {
        _scoreRepository = scoreRepository;
    }

    public async Task<Result<List<CoachLeaderboardEntryDto>>> Handle(
        GetCoachLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var scores = await _scoreRepository.GetByGymAsync(
            request.GymId, now.Month, now.Year, cancellationToken);

        var entries = scores
            .OrderByDescending(s => s.NormalizedScore)
            .Select(s => new CoachLeaderboardEntryDto(
                s.TrainerId,
                s.NormalizedScore,
                s.TotalRatings,
                s.UniqueClients,
                s.SessionCount,
                s.IsCoachOfTheMonth))
            .ToList();

        return Result.Success(entries);
    }
}

using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Repositories;

namespace GymApp.Modules.Gamification.Application.Queries.GetCoachOfTheMonth;

internal sealed class GetCoachOfTheMonthQueryHandler
    : IQueryHandler<GetCoachOfTheMonthQuery, CoachOfTheMonthResponse>
{
    private readonly ITrainerMonthlyScoreRepository _scoreRepository;

    public GetCoachOfTheMonthQueryHandler(ITrainerMonthlyScoreRepository scoreRepository)
    {
        _scoreRepository = scoreRepository;
    }

    public async Task<Result<CoachOfTheMonthResponse>> Handle(
        GetCoachOfTheMonthQuery request,
        CancellationToken cancellationToken)
    {
        var coach = await _scoreRepository.GetCoachOfMonthAsync(
            request.GymId, request.Month, request.Year, cancellationToken);

        if (coach is null)
        {
            return Result.Success(new CoachOfTheMonthResponse(
                TrainerId: null,
                Month: request.Month,
                Year: request.Year,
                NormalizedScore: null,
                TotalRatings: null,
                UniqueClients: null,
                SessionCount: null,
                HasCoach: false));
        }

        return Result.Success(new CoachOfTheMonthResponse(
            coach.TrainerId,
            coach.Month,
            coach.Year,
            coach.NormalizedScore,
            coach.TotalRatings,
            coach.UniqueClients,
            coach.SessionCount,
            HasCoach: true));
    }
}

using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Application.Queries.GetMyLeaderboardPosition;

internal sealed class GetMyLeaderboardPositionQueryHandler
    : IQueryHandler<GetMyLeaderboardPositionQuery, MyLeaderboardPositionResponse>
{
    private readonly IMemberLeaderboardRepository _leaderboardRepository;
    private readonly ILeaderboardOptOutRepository _optOutRepository;

    public GetMyLeaderboardPositionQueryHandler(
        IMemberLeaderboardRepository leaderboardRepository,
        ILeaderboardOptOutRepository optOutRepository)
    {
        _leaderboardRepository = leaderboardRepository;
        _optOutRepository = optOutRepository;
    }

    public async Task<Result<MyLeaderboardPositionResponse>> Handle(
        GetMyLeaderboardPositionQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var period = LeaderboardPeriod.Create(now.Month, now.Year);

        var optOut = await _optOutRepository.GetByUserAndGymAsync(
            request.UserId, request.GymId, cancellationToken);

        bool isOptedIn = optOut?.ShowOnLeaderboard ?? true;

        var positions = new List<LeaderboardPositionDto>();

        foreach (var type in Enum.GetValues<LeaderboardType>())
        {
            var leaderboard = await _leaderboardRepository.GetByGymAndTypeAsync(
                request.GymId, type, period, cancellationToken);

            if (leaderboard is null)
            {
                positions.Add(new LeaderboardPositionDto(
                    type.ToString(),
                    null,
                    null,
                    null,
                    isOptedIn));
                continue;
            }

            var entry = leaderboard.Entries.FirstOrDefault(e => e.MemberId == request.UserId);

            positions.Add(new LeaderboardPositionDto(
                type.ToString(),
                entry?.Rank,
                entry?.Score,
                entry?.Delta,
                isOptedIn));
        }

        return Result.Success(new MyLeaderboardPositionResponse(positions));
    }
}

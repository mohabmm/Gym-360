using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Application.Queries.GetMemberLeaderboard;

internal sealed class GetMemberLeaderboardQueryHandler : IQueryHandler<GetMemberLeaderboardQuery, LeaderboardResponse>
{
    private readonly IMemberLeaderboardRepository _leaderboardRepository;
    private readonly IUserDisplayLookup _userDisplayLookup;

    public GetMemberLeaderboardQueryHandler(
        IMemberLeaderboardRepository leaderboardRepository,
        IUserDisplayLookup userDisplayLookup)
    {
        _leaderboardRepository = leaderboardRepository;
        _userDisplayLookup = userDisplayLookup;
    }

    public async Task<Result<LeaderboardResponse>> Handle(GetMemberLeaderboardQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LeaderboardType>(request.Type, ignoreCase: true, out var leaderboardType))
        {
            return Result.Failure<LeaderboardResponse>(GamificationErrors.LeaderboardNotFound);
        }

        var now = DateTime.UtcNow;
        int month = request.Month ?? now.Month;
        int year = request.Year ?? now.Year;
        var period = LeaderboardPeriod.Create(month, year);
        var periodString = $"{period.Year:D4}-{period.Month:D2}";
        var typeKey = leaderboardType.ToString();

        var leaderboard = await _leaderboardRepository.GetByGymAndTypeAsync(
            request.GymId, leaderboardType, period, cancellationToken);

        if (leaderboard is null)
        {
            return Result.Success(new LeaderboardResponse(
                typeKey,
                periodString,
                Array.Empty<LeaderboardEntryDto>(),
                MyPosition: null,
                TotalParticipants: 0,
                OptedOutCount: 0));
        }

        var optedInEntries = leaderboard.Entries
            .Where(e => e.IsOptedIn)
            .OrderBy(e => e.Rank)
            .ToList();

        var optedOutCount = leaderboard.Entries.Count(e => !e.IsOptedIn);

        var memberIds = new HashSet<Guid>(optedInEntries.Select(e => e.MemberId));
        if (request.RequestingUserId is Guid me)
        {
            memberIds.Add(me);
        }

        var displayNames = await _userDisplayLookup.GetDisplayNamesAsync(memberIds, cancellationToken);

        LeaderboardEntryDto ToDto(Domain.Entities.LeaderboardEntry entry)
        {
            var display = displayNames.TryGetValue(entry.MemberId, out var d) ? d : null;
            return new LeaderboardEntryDto(
                Position: entry.Rank,
                MemberId: entry.MemberId,
                MemberName: display?.FullName ?? string.Empty,
                MemberPhotoUrl: display?.PhotoUrl,
                Score: entry.Score,
                ScoreLabel: FormatScoreLabel(leaderboardType, entry.Score));
        }

        var entryDtos = optedInEntries.Select(ToDto).ToList();

        LeaderboardEntryDto? myPosition = null;
        if (request.RequestingUserId is Guid userId)
        {
            var myEntry = leaderboard.Entries.FirstOrDefault(e => e.MemberId == userId);
            if (myEntry is not null)
            {
                myPosition = ToDto(myEntry);
            }
        }

        return Result.Success(new LeaderboardResponse(
            typeKey,
            periodString,
            entryDtos,
            myPosition,
            TotalParticipants: optedInEntries.Count,
            OptedOutCount: optedOutCount));
    }

    private static string FormatScoreLabel(LeaderboardType type, decimal score) => type switch
    {
        LeaderboardType.Attendance => $"{score:0} sessions",
        LeaderboardType.WeightLoss => $"{score:0.0}% lost",
        LeaderboardType.Consistency => $"{score:0}%",
        _ => score.ToString("0.##"),
    };
}

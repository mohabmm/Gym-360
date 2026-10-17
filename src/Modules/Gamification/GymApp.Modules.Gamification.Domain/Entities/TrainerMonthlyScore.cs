using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.DomainEvents;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class TrainerMonthlyScore : Entity
{
    private const int MinUniqueClientsForCoachOfMonth = 3;

    private TrainerMonthlyScore() { }

    private TrainerMonthlyScore(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal AggregatedScore { get; private set; }
    public int TotalRatings { get; private set; }
    public int UniqueClients { get; private set; }
    public int SessionCount { get; private set; }
    public decimal NormalizedScore { get; private set; }
    public bool IsCoachOfTheMonth { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    public static Result<TrainerMonthlyScore> Create(TrainerMonthlyScoreDetails details)
    {
        var score = new TrainerMonthlyScore(Guid.NewGuid())
        {
            GymId = details.GymId,
            TrainerId = details.TrainerId,
            Month = details.Month,
            Year = details.Year,
            AggregatedScore = details.AggregatedScore,
            TotalRatings = details.TotalRatings,
            UniqueClients = details.UniqueClients,
            SessionCount = details.SessionCount,
            NormalizedScore = 0m,
            IsCoachOfTheMonth = false,
            CalculatedAt = DateTime.UtcNow
        };

        score.RecalculateNormalizedScore(0m, 0m);

        return Result.Success(score);
    }

    /// <summary>
    /// Recalculates the normalized score.
    /// Formula: (avgStars * 0.6) + (consistencyBonus * 0.2) + (clientRetentionRate * 0.2)
    /// </summary>
    public void RecalculateNormalizedScore(decimal consistencyBonus, decimal clientRetentionRate)
    {
        decimal avgStars = TotalRatings > 0
            ? AggregatedScore / TotalRatings
            : 0m;

        NormalizedScore = (avgStars * 0.6m)
            + (consistencyBonus * 0.2m)
            + (clientRetentionRate * 0.2m);

        CalculatedAt = DateTime.UtcNow;
    }

    public Result SelectAsCoachOfTheMonth()
    {
        if (UniqueClients < MinUniqueClientsForCoachOfMonth)
        {
            return Result.Failure(GamificationErrors.InsufficientRatingsForCoachOfMonth);
        }

        IsCoachOfTheMonth = true;
        CalculatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CoachOfTheMonthSelectedDomainEvent(
            TrainerId,
            GymId,
            Month,
            Year));

        return Result.Success();
    }

    public void UpdateScores(
        decimal aggregatedScore,
        int totalRatings,
        int uniqueClients,
        int sessionCount)
    {
        AggregatedScore = aggregatedScore;
        TotalRatings = totalRatings;
        UniqueClients = uniqueClients;
        SessionCount = sessionCount;
        CalculatedAt = DateTime.UtcNow;
    }
}

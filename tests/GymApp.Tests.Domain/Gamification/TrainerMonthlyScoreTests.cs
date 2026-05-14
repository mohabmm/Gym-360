using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.DomainEvents;
using GymApp.Modules.Gamification.Domain.Entities;

namespace GymApp.Tests.Domain.Gamification;

public sealed class TrainerMonthlyScoreTests
{
    private static Result<TrainerMonthlyScore> CreateDefaultScore(
        decimal aggregatedScore = 20m,
        int totalRatings = 5,
        int uniqueClients = 5,
        int sessionCount = 10)
    {
        return TrainerMonthlyScore.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            6,
            2025,
            aggregatedScore,
            totalRatings,
            uniqueClients,
            sessionCount);
    }

    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var gymId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();

        var result = TrainerMonthlyScore.Create(gymId, trainerId, 3, 2025, 22.5m, 5, 4, 12);

        result.IsSuccess.Should().BeTrue();
        var score = result.Value;
        score.GymId.Should().Be(gymId);
        score.TrainerId.Should().Be(trainerId);
        score.Month.Should().Be(3);
        score.Year.Should().Be(2025);
        score.AggregatedScore.Should().Be(22.5m);
        score.TotalRatings.Should().Be(5);
        score.UniqueClients.Should().Be(4);
        score.SessionCount.Should().Be(12);
        score.IsCoachOfTheMonth.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCalculateInitialNormalizedScore()
    {
        var result = CreateDefaultScore(aggregatedScore: 20m, totalRatings: 5);

        // avgStars = 20/5 = 4.0
        // normalized = (4.0 * 0.6) + (0 * 0.2) + (0 * 0.2) = 2.4
        result.Value.NormalizedScore.Should().Be(2.4m);
    }

    [Fact]
    public void RecalculateNormalizedScore_ShouldApplyFormula()
    {
        var score = CreateDefaultScore(aggregatedScore: 22.5m, totalRatings: 5).Value;

        // avgStars = 22.5/5 = 4.5
        score.RecalculateNormalizedScore(consistencyBonus: 0.8m, clientRetentionRate: 0.9m);

        // (4.5 * 0.6) + (0.8 * 0.2) + (0.9 * 0.2) = 2.7 + 0.16 + 0.18 = 3.04
        score.NormalizedScore.Should().Be(3.04m);
    }

    [Fact]
    public void RecalculateNormalizedScore_ZeroRatings_ShouldBeZeroAvg()
    {
        var score = CreateDefaultScore(aggregatedScore: 0m, totalRatings: 0).Value;

        score.RecalculateNormalizedScore(1.0m, 1.0m);

        // (0 * 0.6) + (1.0 * 0.2) + (1.0 * 0.2) = 0 + 0.2 + 0.2 = 0.4
        score.NormalizedScore.Should().Be(0.4m);
    }

    // --- Coach of the Month ---

    [Fact]
    public void SelectAsCoachOfTheMonth_WithEnoughClients_ShouldSucceed()
    {
        var score = CreateDefaultScore(uniqueClients: 5).Value;

        var result = score.SelectAsCoachOfTheMonth();

        result.IsSuccess.Should().BeTrue();
        score.IsCoachOfTheMonth.Should().BeTrue();
        score.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<CoachOfTheMonthSelectedDomainEvent>();
    }

    [Fact]
    public void SelectAsCoachOfTheMonth_Exactly3Clients_ShouldSucceed()
    {
        var score = CreateDefaultScore(uniqueClients: 3).Value;

        var result = score.SelectAsCoachOfTheMonth();

        result.IsSuccess.Should().BeTrue();
        score.IsCoachOfTheMonth.Should().BeTrue();
    }

    [Fact]
    public void SelectAsCoachOfTheMonth_InsufficientClients_ShouldFail()
    {
        var score = CreateDefaultScore(uniqueClients: 2).Value;

        var result = score.SelectAsCoachOfTheMonth();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gamification.InsufficientRatingsForCoachOfMonth");
        score.IsCoachOfTheMonth.Should().BeFalse();
    }

    [Fact]
    public void SelectAsCoachOfTheMonth_ShouldRaiseCoachOfMonthEvent()
    {
        var score = CreateDefaultScore(uniqueClients: 4).Value;

        score.SelectAsCoachOfTheMonth();

        var evt = score.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<CoachOfTheMonthSelectedDomainEvent>().Subject;

        evt.TrainerId.Should().Be(score.TrainerId);
        evt.GymId.Should().Be(score.GymId);
        evt.Month.Should().Be(6);
        evt.Year.Should().Be(2025);
    }

    // --- UpdateScores ---

    [Fact]
    public void UpdateScores_ShouldReplaceAllMetrics()
    {
        var score = CreateDefaultScore().Value;

        score.UpdateScores(30m, 8, 6, 15);

        score.AggregatedScore.Should().Be(30m);
        score.TotalRatings.Should().Be(8);
        score.UniqueClients.Should().Be(6);
        score.SessionCount.Should().Be(15);
    }
}

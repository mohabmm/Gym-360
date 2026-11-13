using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Application.Commands.SetProgressGoal;
using GymApp.Modules.Progress.Application.Queries.GetClientProgress;
using GymApp.Modules.Progress.Application.Queries.GetProgressJourney;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Tests.Application.Progress;

/// <summary>pt.progress_measurements: journey (day one → now), member goal, coach-only-own-clients.</summary>
public sealed class ProgressJourneyHandlerTests
{
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _gym = Guid.NewGuid();
    private readonly Guid _coach = Guid.NewGuid();

    private readonly IBodyMetricEntryRepository _metrics = Substitute.For<IBodyMetricEntryRepository>();
    private readonly IProgressGoalRepository _goals = Substitute.For<IProgressGoalRepository>();
    private readonly IProgressPhotoRepository _photos = Substitute.For<IProgressPhotoRepository>();
    private readonly IStreakRepository _streaks = Substitute.For<IStreakRepository>();
    private readonly IProgressUnitOfWork _unitOfWork = Substitute.For<IProgressUnitOfWork>();
    private readonly IGymPlanService _plan = Substitute.For<IGymPlanService>();
    private readonly ITrainerAssignmentLookup _assignments = Substitute.For<ITrainerAssignmentLookup>();

    public ProgressJourneyHandlerTests()
    {
        _plan.IsFeatureEnabledAsync(_gym, "progress_tracking", Arg.Any<CancellationToken>()).Returns(true);
        _assignments.GetClientsForTrainerAsync(_coach, _gym, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { _member });
    }

    private GetProgressJourneyQueryHandler Journey() => new(_metrics, _goals, _plan, _assignments);
    private SetProgressGoalCommandHandler SetGoal() => new(_goals, _unitOfWork, _plan, _assignments);

    private BodyMetricEntry Entry(string date, decimal weight, MetricSource source) =>
        BodyMetricEntry.Create(new BodyMetricEntryDetails
        {
            UserId = _member,
            GymId = _gym,
            Date = DateOnly.Parse(date),
            Source = source,
            LoggedBy = source == MetricSource.TrainerMeasured ? _coach : _member,
            Reading = new BodyMetricReading { Weight = weight, WeightUnit = WeightUnit.Kg },
        }).Value;

    private void HasEntries(params BodyMetricEntry[] entries) =>
        _metrics.GetByUserAsync(_member, _gym, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(entries.ToList());

    [Fact]
    public async Task Member_gets_every_entry_since_day_one_oldest_first_with_goal()
    {
        HasEntries(
            Entry("2026-11-01", 88m, MetricSource.SelfReported),
            Entry("2026-09-01", 95m, MetricSource.TrainerMeasured),
            Entry("2026-10-01", 91m, MetricSource.TrainerMeasured));
        _goals.FindAsync(_member, _gym, Arg.Any<CancellationToken>())
            .Returns(ProgressGoal.Create(_member, _gym, new ProgressGoalChoice(ProgressGoalType.LoseFat, 80m, _coach)).Value);

        var result = await Journey().Handle(new GetProgressJourneyQuery(_member, _gym, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Select(e => e.Weight).Should().Equal(95m, 91m, 88m);
        result.Value.Entries[0].Source.Should().Be("TrainerMeasured");
        result.Value.Goal!.Goal.Should().Be("LoseFat");
        result.Value.Goal.TargetWeight.Should().Be(80m);
    }

    [Fact]
    public async Task Member_without_entries_or_goal_gets_an_empty_journey()
    {
        HasEntries();

        var result = await Journey().Handle(new GetProgressJourneyQuery(_member, _gym, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().BeEmpty();
        result.Value.Goal.Should().BeNull();
    }

    [Fact]
    public async Task Coach_reads_own_client_journey()
    {
        HasEntries(Entry("2026-09-01", 95m, MetricSource.TrainerMeasured));

        var result = await Journey().Handle(new GetProgressJourneyQuery(_member, _gym, _coach), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Other_coach_is_forbidden_and_nothing_is_read()
    {
        var otherCoach = Guid.NewGuid();
        _assignments.GetClientsForTrainerAsync(otherCoach, _gym, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var result = await Journey().Handle(new GetProgressJourneyQuery(_member, _gym, otherCoach), default);

        result.Error.Should().Be(ProgressErrors.NotClientOfTrainer);
        await _metrics.DidNotReceiveWithAnyArgs().GetByUserAsync(default, default, default!, default);
    }

    [Fact]
    public async Task Feature_off_returns_feature_not_available_before_any_read()
    {
        _plan.IsFeatureEnabledAsync(_gym, "progress_tracking", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Journey().Handle(new GetProgressJourneyQuery(_member, _gym, null), default);

        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _metrics.DidNotReceiveWithAnyArgs().GetByUserAsync(default, default, default!, default);
    }

    [Fact]
    public async Task Coach_sets_a_new_goal_for_a_client()
    {
        var result = await SetGoal().Handle(new SetProgressGoalCommand(_member, _gym, _coach, "losefat", 80m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Goal.Should().Be("LoseFat");
        result.Value.SetBy.Should().Be(_coach);
        await _goals.Received(1).AddAsync(Arg.Is<ProgressGoal>(g => g.UserId == _member && g.TargetWeight == 80m), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Member_changes_existing_goal_in_place()
    {
        var existing = ProgressGoal.Create(_member, _gym, new ProgressGoalChoice(ProgressGoalType.LoseFat, 80m, _coach)).Value;
        _goals.GetAsync(_member, _gym, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await SetGoal().Handle(new SetProgressGoalCommand(_member, _gym, null, "BuildMuscle", null), default);

        result.IsSuccess.Should().BeTrue();
        existing.Goal.Should().Be(ProgressGoalType.BuildMuscle);
        existing.SetBy.Should().Be(_member);
        await _goals.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Other_coach_cannot_set_goal()
    {
        var result = await SetGoal().Handle(new SetProgressGoalCommand(_member, _gym, Guid.NewGuid(), "LoseFat", null), default);

        result.Error.Should().Be(ProgressErrors.NotClientOfTrainer);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData("Bulk")]
    [InlineData("7")]
    public async Task Unknown_goal_is_rejected(string goal)
    {
        var result = await SetGoal().Handle(new SetProgressGoalCommand(_member, _gym, null, goal, null), default);

        result.Error.Should().Be(ProgressErrors.InvalidGoal);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Invalid_target_weight_is_not_saved()
    {
        var result = await SetGoal().Handle(new SetProgressGoalCommand(_member, _gym, _coach, "LoseFat", 0m), default);

        result.Error.Should().Be(ProgressErrors.InvalidTargetWeight);
        await _goals.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Client_progress_summary_is_forbidden_for_a_coach_who_is_not_assigned()
    {
        var handler = new GetClientProgressQueryHandler(_metrics, _photos, _streaks, _assignments);
        var otherCoach = Guid.NewGuid();
        _assignments.GetClientsForTrainerAsync(otherCoach, _gym, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var result = await handler.Handle(new GetClientProgressQuery(otherCoach, _member, _gym), default);

        result.Error.Should().Be(ProgressErrors.NotClientOfTrainer);
        await _metrics.DidNotReceiveWithAnyArgs().GetByUserAsync(default, default, default!, default);
    }

    [Fact]
    public async Task Client_progress_summary_still_works_for_the_assigned_coach()
    {
        var handler = new GetClientProgressQueryHandler(_metrics, _photos, _streaks, _assignments);
        HasEntries(Entry("2026-09-01", 95m, MetricSource.TrainerMeasured), Entry("2026-10-01", 91m, MetricSource.TrainerMeasured));

        var result = await handler.Handle(new GetClientProgressQuery(_coach, _member, _gym), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.WeightChangeFromFirst.Should().Be(-4m);
    }
}

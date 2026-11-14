using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Application.Scheduling;

/// <summary>pt.exercise_progress: who may read a member's board, and the coach's day label on logging.</summary>
public sealed class GetExerciseProgressQueryHandlerTests
{
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();

    private readonly ISessionRepository _sessions = Substitute.For<ISessionRepository>();
    private readonly IPtProgressScopeLookup _scope = Substitute.For<IPtProgressScopeLookup>();
    private readonly ITrainerAssignmentLookup _assignments = Substitute.For<ITrainerAssignmentLookup>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly GetExerciseProgressQueryHandler _sut;

    public GetExerciseProgressQueryHandlerTests()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtExerciseProgress, Arg.Any<CancellationToken>()).Returns(true);
        _scope.GetScopeAsync(Member, Gym, Arg.Any<CancellationToken>()).Returns(PtProgressScope.None);
        _sessions.GetCompletedWithExerciseLogsByMemberAsync(new MemberInGym(Gym, Member), Arg.Any<CancellationToken>())
            .Returns(new List<Session> { LoggedSession() });
        _assignments.GetClientsForTrainerAsync(Arg.Any<Guid>(), Gym, Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        _sut = new GetExerciseProgressQueryHandler(_sessions, _scope, _assignments, _flags);
    }

    private static Session LoggedSession()
    {
        var session = Session.LogCompleted(
            new SessionParticipants(Gym, Coach, Member),
            new SessionSchedule(new DateOnly(2026, 11, 10), new TimeOnly(18, 0), 60),
            notes: null);
        session.AddExerciseLog(new ExerciseLogDetails { ExerciseName = "Squat", OrderIndex = 0, Sets = 3, Reps = "5", Weight = 80m });
        return session;
    }

    [Fact]
    public async Task Feature_off_is_refused_before_any_read()
    {
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtExerciseProgress, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(new GetExerciseProgressQuery(Member, Member, Gym), CancellationToken.None);

        result.Error.Should().Be(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtExerciseProgress));
        await _sessions.DidNotReceiveWithAnyArgs().GetCompletedWithExerciseLogsByMemberAsync(default!, default);
    }

    [Fact]
    public async Task Member_reads_their_own_board()
    {
        var result = await _sut.Handle(new GetExerciseProgressQuery(Member, Member, Gym), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Exercises.Should().ContainSingle(e => e.Name == "Squat");
    }

    [Fact]
    public async Task Coach_who_trained_the_member_reads_the_board()
    {
        _sessions.HasSessionWithMemberAsync(new SessionParticipants(Gym, Coach, Member), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(new GetExerciseProgressQuery(Coach, Member, Gym), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Coach_of_the_active_package_reads_the_board_before_any_session()
    {
        _assignments.GetClientsForTrainerAsync(Coach, Gym, Arg.Any<CancellationToken>()).Returns(new List<Guid> { Member });

        var result = await _sut.Handle(new GetExerciseProgressQuery(Coach, Member, Gym), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Anyone_else_is_forbidden()
    {
        var result = await _sut.Handle(new GetExerciseProgressQuery(Guid.NewGuid(), Member, Gym), CancellationToken.None);

        result.Error.Should().Be(SchedulingErrors.ExerciseProgressForbidden);
    }

    [Fact]
    public async Task Logging_exercises_with_a_day_label_labels_the_session()
    {
        var session = Session.LogCompleted(
            new SessionParticipants(Gym, Coach, Member),
            new SessionSchedule(new DateOnly(2026, 11, 10), new TimeOnly(18, 0), 60),
            notes: null);
        _sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var handler = new LogSessionExercisesCommandHandler(_sessions, Substitute.For<ISchedulingUnitOfWork>());
        var item = new ExerciseLogItem(null, "Bench press", null, 0, 3, "8", 60m, null, new ExerciseTarget(3, "8", 62.5m));

        var result = await handler.Handle(
            new LogSessionExercisesCommand(session.Id, Coach, new[] { item }, "Push Day"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.DayLabel.Should().Be("Push Day");
        result.Value.Single().Target.Should().Be(new ExerciseTarget(3, "8", 62.5m));
    }
}

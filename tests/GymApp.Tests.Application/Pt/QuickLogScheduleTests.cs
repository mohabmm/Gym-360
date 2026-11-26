using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Common.Application.Messaging;

namespace GymApp.Tests.Application.Pt;

/// <summary>
/// pt.dual_session_logging: the coach says when the session happened — today (default: going on
/// now, so the coach can record it while training) or up to 7 days back — and the same slot is never recorded twice.
/// Clock: 12 Nov 2026 16:00 UTC = 18:00 in Cairo.
/// </summary>
public class QuickLogScheduleTests
{
    private readonly ISessionRepository _sessions = Substitute.For<ISessionRepository>();
    private readonly ISessionPackageService _packages = Substitute.For<ISessionPackageService>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IIntegrationEventPublisher _events = Substitute.For<IIntegrationEventPublisher>();
    private readonly ISchedulingUnitOfWork _uow = Substitute.For<ISchedulingUnitOfWork>();
    private readonly QuickLogSessionCommandHandler _sut;

    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Client = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 12);

    public QuickLogScheduleTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 11, 12, 16, 0, 0, DateTimeKind.Utc));
        _flags.IsEnabledAsync(Gym, FeatureKeys.PtDualSessionLogging, Arg.Any<CancellationToken>()).Returns(true);
        _packages.GetRemainingWithTrainerAsync(Client, Coach, Gym, Arg.Any<CancellationToken>()).Returns(5);
        _sut = new QuickLogSessionCommandHandler(new QuickLogStateLoader(_sessions, _packages, _clock), _sessions, _flags, _events, _uow);
    }

    private static QuickLogSessionCommand At(DateOnly? date, TimeOnly? start, int? minutes = null) =>
        new(Gym, Coach, Client, null, date, start, minutes);

    private async Task<Session> SavedAsync(QuickLogSessionCommand command)
    {
        Session? saved = null;
        await _sessions.AddAsync(Arg.Do<Session>(s => saved = s), Arg.Any<CancellationToken>());
        var result = await _sut.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Name : "");
        return saved!;
    }

    [Fact]
    public async Task Without_a_time_it_is_a_session_going_on_now()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 11, 12, 16, 10, 0, DateTimeKind.Utc));

        var saved = await SavedAsync(At(null, null));

        saved.ScheduledDate.Should().Be(Today);
        saved.ScheduledTime.Should().Be(new TimeOnly(18, 0), "18:10 now, at the current quarter hour");
        saved.DurationMinutes.Should().Be(60);
    }

    [Fact]
    public async Task A_forgotten_session_from_yesterday_is_recorded_on_that_day()
    {
        var saved = await SavedAsync(At(Today.AddDays(-1), new TimeOnly(19, 30), 45));

        saved.ScheduledDate.Should().Be(Today.AddDays(-1));
        saved.ScheduledTime.Should().Be(new TimeOnly(19, 30));
        saved.DurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task Seven_days_back_is_allowed_eight_is_not()
    {
        (await SavedAsync(At(Today.AddDays(-7), new TimeOnly(10, 0)))).ScheduledDate.Should().Be(Today.AddDays(-7));

        var result = await _sut.Handle(At(Today.AddDays(-8), new TimeOnly(10, 0)), CancellationToken.None);

        result.Error.Should().Be(SchedulingErrors.QuickLogTooOld);
    }

    [Fact]
    public async Task A_session_in_progress_is_recorded()
    {
        var saved = await SavedAsync(At(Today, new TimeOnly(17, 30), 60));

        saved.ScheduledTime.Should().Be(new TimeOnly(17, 30), "it started 30 minutes ago and is still going");
    }

    [Fact]
    public async Task A_session_starting_within_five_minutes_is_recorded() =>
        (await SavedAsync(At(Today, new TimeOnly(18, 5)))).ScheduledTime.Should().Be(new TimeOnly(18, 5));

    [Fact]
    public async Task A_session_that_has_not_started_yet_is_rejected()
    {
        var result = await _sut.Handle(At(Today, new TimeOnly(18, 30), 60), CancellationToken.None);

        result.Error.Should().Be(SchedulingErrors.QuickLogNotStarted);
        await _sessions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Recording_the_same_slot_again_returns_the_existing_session()
    {
        var schedule = new SessionSchedule(Today.AddDays(-1), new TimeOnly(19, 30), 60);
        var existing = Session.LogCompleted(new SessionParticipants(Gym, Coach, Client), schedule, null);
        _sessions.FindQuickLogAtAsync(
                Arg.Is<SessionParticipants>(p => p.MemberId == Client),
                Arg.Is<SessionSchedule>(s => s.Date == schedule.Date && s.Time == schedule.Time),
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.Handle(At(schedule.Date, schedule.Time), CancellationToken.None);

        result.Value.AlreadyLogged.Should().BeTrue();
        result.Value.SessionId.Should().Be(existing.Id);
        result.Value.Date.Should().Be(schedule.Date);
        result.Value.StartTime.Should().Be(schedule.Time);
        await _sessions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Two_different_backdated_sessions_in_a_row_are_both_recorded()
    {
        // The 5-minute double-tap guard only applies to one-tap logs without a time.
        _sessions.FindRecentQuickLogAsync(Coach, Client, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Session.LogCompleted(new SessionParticipants(Gym, Coach, Client), new SessionSchedule(Today, new TimeOnly(9, 0), 60), null));

        var saved = await SavedAsync(At(Today.AddDays(-2), new TimeOnly(9, 0)));

        saved.ScheduledDate.Should().Be(Today.AddDays(-2));
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(15, true)]
    [InlineData(180, true)]
    [InlineData(181, false)]
    public void Duration_must_be_15_to_180_minutes(int minutes, bool valid) =>
        new QuickLogSessionCommandValidator().Validate(At(Today, new TimeOnly(9, 0), minutes)).IsValid.Should().Be(valid);

    [Fact]
    public void A_date_needs_a_start_time() =>
        new QuickLogSessionCommandValidator().Validate(At(Today.AddDays(-1), null)).IsValid.Should().BeFalse();
}

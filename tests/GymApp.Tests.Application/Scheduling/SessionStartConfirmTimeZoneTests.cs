using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Commands.ConfirmSession;
using GymApp.Modules.Scheduling.Application.Commands.StartSession;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Application.Scheduling;

/// <summary>
/// Sessions store their scheduled time as gym-local (Africa/Cairo) wall-clock. The
/// start/confirm guards used to compare that value against raw UTC, which skewed them
/// by Egypt's UTC offset: a trainer could not start a session until 2–3 hours after it
/// was due, and a booking request stayed confirmable hours after its time had passed.
/// </summary>
public class SessionStartConfirmTimeZoneTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly IGymPlanService _gymPlanService = Substitute.For<IGymPlanService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly Guid GymId = Guid.NewGuid();

    // 2026-10-12 is in Egyptian summer time (UTC+3): 21:00 Cairo == 18:00 UTC.
    private static readonly DateOnly SessionDate = new(2026, 10, 12);
    private static readonly TimeOnly SessionTime = new(21, 0);

    private static DateTime Utc(int hour, int minute) =>
        new(2026, 10, 12, hour, minute, 0, DateTimeKind.Utc);

    private Session ArrangeSession(bool confirmed)
    {
        var session = Session.Create(
            new SessionParticipants(GymId, Guid.NewGuid(), Guid.NewGuid()),
            new SessionSchedule(SessionDate, SessionTime, 60),
            SessionType.PersonalTraining,
            "General");
        if (confirmed)
            session.Confirm(new DateTime(2026, 10, 1)).IsSuccess.Should().BeTrue();

        _sessionRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _gymPlanService
            .IsFeatureEnabledAsync(GymId, "early_session_start", Arg.Any<CancellationToken>())
            .Returns(false);
        return session;
    }

    private StartSessionCommandHandler StartHandler() =>
        new(_sessionRepository, _unitOfWork, _gymPlanService, _clock);

    private ConfirmSessionCommandHandler ConfirmHandler() =>
        new(_sessionRepository, _unitOfWork, _clock);

    [Fact]
    public async Task Start_TenMinutesBeforeCairoStartTime_Succeeds()
    {
        var session = ArrangeSession(confirmed: true);
        _clock.UtcNow.Returns(Utc(17, 50)); // 20:50 Cairo — inside the 15-min early window

        var result = await StartHandler().Handle(new StartSessionCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public async Task Start_AtCairoStartTime_Succeeds()
    {
        var session = ArrangeSession(confirmed: true);
        _clock.UtcNow.Returns(Utc(18, 0)); // 21:00 Cairo

        var result = await StartHandler().Handle(new StartSessionCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Start_AnHourBeforeCairoStartTime_IsTooEarly()
    {
        var session = ArrangeSession(confirmed: true);
        _clock.UtcNow.Returns(Utc(17, 0)); // 20:00 Cairo

        var result = await StartHandler().Handle(new StartSessionCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.SessionTooEarly");
        session.Status.Should().Be(SessionStatus.Confirmed);
    }

    [Fact]
    public async Task Confirm_BeforeCairoStartTime_Succeeds()
    {
        var session = ArrangeSession(confirmed: false);
        _clock.UtcNow.Returns(Utc(17, 50)); // 20:50 Cairo

        var result = await ConfirmHandler().Handle(new ConfirmSessionCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Confirmed);
    }

    [Fact]
    public async Task Confirm_AfterCairoStartTimeHasPassed_IsRejected()
    {
        var session = ArrangeSession(confirmed: false);
        _clock.UtcNow.Returns(Utc(18, 30)); // 21:30 Cairo — session time already passed

        var result = await ConfirmHandler().Handle(new ConfirmSessionCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.SessionInPast");
        session.Status.Should().Be(SessionStatus.Pending);
    }
}

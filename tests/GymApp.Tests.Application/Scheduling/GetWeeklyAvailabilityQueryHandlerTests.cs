using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetWeeklyAvailability;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Application.Scheduling;

public class GetWeeklyAvailabilityQueryHandlerTests
{
    private static readonly DateOnly Monday = new(2026, 5, 11);

    private readonly ITrainerAvailabilityRepository _availabilityRepository =
        Substitute.For<ITrainerAvailabilityRepository>();
    private readonly ISessionRepository _sessionRepository =
        Substitute.For<ISessionRepository>();
    private readonly GetWeeklyAvailabilityQueryHandler _sut;

    private readonly Guid _trainerId = Guid.NewGuid();
    private readonly Guid _gymId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();

    public GetWeeklyAvailabilityQueryHandlerTests()
    {
        _sut = new GetWeeklyAvailabilityQueryHandler(_availabilityRepository, _sessionRepository);
    }

    private TrainerAvailability MondayWindow(TimeOnly start, TimeOnly end)
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, start, end);
        return availability;
    }

    private Session MondaySessionAtNine(Guid memberId) => Session.Create(
        new SessionParticipants(_gymId, _trainerId, memberId),
        new SessionSchedule(Monday, new TimeOnly(9, 0), 60),
        SessionType.PersonalTraining);

    private void Given(TrainerAvailability? availability, params Session[] sessions)
    {
        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, Arg.Is<GymDateRange>(r => r.GymId == _gymId), Arg.Any<CancellationToken>())
            .Returns(sessions);
    }

    private Task<Result<WeeklyAvailabilityDto>> Handle(DateOnly start, DateOnly end) =>
        _sut.Handle(new GetWeeklyAvailabilityQuery(_trainerId, _gymId, start, end, _memberId), CancellationToken.None);

    private async Task<DayAvailabilityDto> HandleDay(DateOnly date) =>
        (await Handle(date, date)).Value.Days.Single();

    [Fact]
    public async Task Handle_NoAvailability_ReturnsAllDaysOff()
    {
        Given(null);

        var result = await Handle(Monday, Monday.AddDays(6));

        result.IsSuccess.Should().BeTrue();
        result.Value.Days.Should().HaveCount(7);
        result.Value.Days.Should().AllSatisfy(d =>
        {
            d.IsOffDay.Should().BeTrue();
            d.IsBlocked.Should().BeFalse();
            d.Slots.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Handle_BlockedDate_MarksDayBlockedWithReason()
    {
        var availability = MondayWindow(new TimeOnly(9, 0), new TimeOnly(17, 0));
        availability.BlockDate(Monday, "Vacation");
        Given(availability);

        var day = await HandleDay(Monday);

        day.IsBlocked.Should().BeTrue();
        day.IsOffDay.Should().BeFalse();
        day.BlockedReason.Should().Be("Vacation");
        day.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DayWithoutWeeklySlot_IsOffDay()
    {
        Given(MondayWindow(new TimeOnly(9, 0), new TimeOnly(11, 0)));

        var day = await HandleDay(Monday.AddDays(1)); // Tuesday

        day.IsOffDay.Should().BeTrue();
        day.Slots.Should().BeEmpty();
    }

    // Regression: TimeOnly.AddMinutes wraps past midnight, so a window ending late in
    // the evening made the slot loop spin forever.
    [Fact]
    public async Task Handle_WindowEndingNearMidnight_TerminatesWithoutWrapping()
    {
        Given(MondayWindow(new TimeOnly(6, 0), new TimeOnly(23, 59)));

        var handle = Task.Run(() => Handle(Monday, Monday));
        var finished = await Task.WhenAny(handle, Task.Delay(TimeSpan.FromSeconds(5)));

        finished.Should().BeSameAs(handle, "slot generation must terminate");
        var slots = (await handle).Value.Days.Single().Slots;
        slots.Should().NotBeEmpty();
        slots.Count.Should().BeLessThan(24);
        slots.Should().AllSatisfy(s => s.EndTime.Should().BeAfter(s.StartTime));
    }

    [Fact]
    public async Task Handle_AvailableSlots_AllMarkedAvailable()
    {
        Given(MondayWindow(new TimeOnly(9, 0), new TimeOnly(11, 0)));

        var day = await HandleDay(Monday);

        day.IsOffDay.Should().BeFalse();
        day.IsBlocked.Should().BeFalse();
        day.Slots.Should().NotBeEmpty().And.AllSatisfy(s =>
        {
            s.Status.Should().Be("Available");
            s.SessionId.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_SessionForRequestingMember_MarkedBookedByMe()
    {
        var session = MondaySessionAtNine(_memberId);
        Given(MondayWindow(new TimeOnly(9, 0), new TimeOnly(11, 0)), session);

        var firstSlot = (await HandleDay(Monday)).Slots.First();

        firstSlot.Status.Should().Be("BookedByMe");
        firstSlot.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task Handle_SessionForOtherMember_MarkedBookedByOther()
    {
        var session = MondaySessionAtNine(Guid.NewGuid());
        Given(MondayWindow(new TimeOnly(9, 0), new TimeOnly(11, 0)), session);

        var firstSlot = (await HandleDay(Monday)).Slots.First();

        firstSlot.Status.Should().Be("BookedByOther");
        firstSlot.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task Handle_CancelledSessionIgnored_SlotAvailable()
    {
        var session = MondaySessionAtNine(_memberId);
        session.Cancel(new SessionCancellation("Changed mind", _memberId, IsLateCancellation: false));
        Given(MondayWindow(new TimeOnly(9, 0), new TimeOnly(11, 0)), session);

        var day = await HandleDay(Monday);

        day.Slots.First().Status.Should().Be("Available");
    }

    [Fact]
    public async Task Handle_EndBeforeStart_ReturnsFailure()
    {
        var result = await Handle(Monday, Monday.AddDays(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidDateRange");
    }
}

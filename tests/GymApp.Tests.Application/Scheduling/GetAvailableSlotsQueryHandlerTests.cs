using GymApp.Modules.Scheduling.Application.Queries.GetAvailableSlots;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Tests.Application.Scheduling;

public class GetAvailableSlotsQueryHandlerTests
{
    private readonly ITrainerAvailabilityRepository _availabilityRepository =
        Substitute.For<ITrainerAvailabilityRepository>();
    private readonly ISessionRepository _sessionRepository =
        Substitute.For<ISessionRepository>();
    private readonly GetAvailableSlotsQueryHandler _sut;

    private readonly Guid _trainerId = Guid.NewGuid();
    private readonly Guid _gymId = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 5, 11);

    public GetAvailableSlotsQueryHandlerTests()
    {
        _sut = new GetAvailableSlotsQueryHandler(_availabilityRepository, _sessionRepository);
    }

    private void Arrange(TrainerAvailability availability, params Session[] sessions)
    {
        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByDateAndTrainerAsync(_trainerId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(sessions);
    }

    [Fact]
    public async Task Handle_NormalWindow_StepsByDurationPlusBuffer()
    {
        // 60 min session + 15 min buffer, 09:00–12:00 → 09:00, 10:15 (11:30+60 overruns).
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        Arrange(availability);

        var result = await _sut.Handle(
            new GetAvailableSlotsQuery(_trainerId, _gymId, Monday), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(s => s.StartTime).Should().Equal(new TimeOnly(9, 0), new TimeOnly(10, 15));
        result.Value.Should().AllSatisfy(s => s.IsAvailable.Should().BeTrue());
    }

    // Regression: TimeOnly.AddMinutes wraps past midnight, so a window ending late in
    // the evening made the slot loop spin forever (request hung, memory grew unbounded).
    [Theory]
    [InlineData(23, 59)]
    [InlineData(23, 30)]
    [InlineData(23, 0)]
    public async Task Handle_WindowEndingNearMidnight_TerminatesWithoutWrapping(int endHour, int endMinute)
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(6, 0), new TimeOnly(endHour, endMinute));
        Arrange(availability);

        // Task.Run: the pre-fix loop spun synchronously, which would block the test
        // thread before WhenAny could observe the timeout.
        var handle = Task.Run(() => _sut.Handle(
            new GetAvailableSlotsQuery(_trainerId, _gymId, Monday), CancellationToken.None));
        var finished = await Task.WhenAny(handle, Task.Delay(TimeSpan.FromSeconds(5)));

        finished.Should().BeSameAs(handle, "slot generation must terminate");
        var slots = (await handle).Value;
        slots.Should().NotBeEmpty();
        slots.Count.Should().BeLessThan(24, "a day cannot hold more than one 60-minute slot per hour");
        slots.Should().BeInAscendingOrder(s => s.StartTime);
        slots.Should().AllSatisfy(s => s.EndTime.Should().BeAfter(s.StartTime));
        slots.Last().EndTime.Should().BeOnOrBefore(new TimeOnly(endHour, endMinute));
    }

    [Fact]
    public async Task Handle_BookedSession_MarksOverlappingSlotUnavailable()
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        var booked = Session.Create(
            _gymId, _trainerId, Guid.NewGuid(), Monday, new TimeOnly(9, 0),
            60, SessionType.PersonalTraining, "Legs");
        Arrange(availability, booked);

        var result = await _sut.Handle(
            new GetAvailableSlotsQuery(_trainerId, _gymId, Monday), CancellationToken.None);

        result.Value.Single(s => s.StartTime == new TimeOnly(9, 0)).IsAvailable.Should().BeFalse();
        result.Value.Single(s => s.StartTime == new TimeOnly(10, 15)).IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LateEveningSession_MarksItsOwnSlotUnavailable()
    {
        // A 23:00 session's "end + buffer" passes midnight; with TimeOnly math it wrapped
        // to 00:15 and no longer conflicted with its own slot.
        var availability = TrainerAvailability.Create(_trainerId, _gymId, 60, 15);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(21, 45), new TimeOnly(23, 59));
        var booked = Session.Create(
            _gymId, _trainerId, Guid.NewGuid(), Monday, new TimeOnly(21, 45),
            60, SessionType.PersonalTraining, "Core");
        Arrange(availability, booked);

        var result = await _sut.Handle(
            new GetAvailableSlotsQuery(_trainerId, _gymId, Monday), CancellationToken.None);

        result.Value.Select(s => s.StartTime).Should().Equal(new TimeOnly(21, 45));
        result.Value.Single().IsAvailable.Should().BeFalse();
    }
}

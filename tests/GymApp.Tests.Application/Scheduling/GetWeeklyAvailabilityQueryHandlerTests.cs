using GymApp.Modules.Scheduling.Application.Queries.GetWeeklyAvailability;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Tests.Application.Scheduling;

public class GetWeeklyAvailabilityQueryHandlerTests
{
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

    [Fact]
    public async Task Handle_NoAvailability_ReturnsAllDaysOff()
    {
        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns((TrainerAvailability?)null);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Session>());

        var start = new DateOnly(2026, 5, 11); // Monday
        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, start, start.AddDays(6), _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

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
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        var blockedDate = new DateOnly(2026, 5, 11);
        availability.BlockDate(blockedDate, "Vacation");

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Session>());

        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, blockedDate, blockedDate, _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var day = result.Value.Days.Single();
        day.IsBlocked.Should().BeTrue();
        day.IsOffDay.Should().BeFalse();
        day.BlockedReason.Should().Be("Vacation");
        day.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DayWithoutWeeklySlot_IsOffDay()
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Session>());

        var tuesday = new DateOnly(2026, 5, 12);
        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, tuesday, tuesday, _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        var day = result.Value.Days.Single();
        day.IsOffDay.Should().BeTrue();
        day.Slots.Should().BeEmpty();
    }

    // Regression: TimeOnly.AddMinutes wraps past midnight, so a window ending late in
    // the evening made the slot loop spin forever.
    [Fact]
    public async Task Handle_WindowEndingNearMidnight_TerminatesWithoutWrapping()
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(6, 0), new TimeOnly(23, 59));

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Session>());

        var monday = new DateOnly(2026, 5, 11);
        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, monday, monday, _memberId);

        var handle = Task.Run(() => _sut.Handle(query, CancellationToken.None));
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
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Session>());

        var monday = new DateOnly(2026, 5, 11);
        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, monday, monday, _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        var day = result.Value.Days.Single();
        day.IsOffDay.Should().BeFalse();
        day.IsBlocked.Should().BeFalse();
        day.Slots.Should().NotBeEmpty();
        day.Slots.Should().AllSatisfy(s => s.Status.Should().Be("Available"));
        day.Slots.Should().AllSatisfy(s => s.SessionId.Should().BeNull());
    }

    [Fact]
    public async Task Handle_SessionForRequestingMember_MarkedBookedByMe()
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        var monday = new DateOnly(2026, 5, 11);
        var session = Session.Create(
            _gymId, _trainerId, _memberId,
            monday, new TimeOnly(9, 0),
            durationMinutes: 60,
            sessionType: SessionType.PersonalTraining);

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, monday, monday, Arg.Any<CancellationToken>())
            .Returns(new[] { session });

        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, monday, monday, _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        var day = result.Value.Days.Single();
        var firstSlot = day.Slots.First();
        firstSlot.Status.Should().Be("BookedByMe");
        firstSlot.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task Handle_SessionForOtherMember_MarkedBookedByOther()
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        var monday = new DateOnly(2026, 5, 11);
        var otherMember = Guid.NewGuid();
        var session = Session.Create(
            _gymId, _trainerId, otherMember,
            monday, new TimeOnly(9, 0),
            durationMinutes: 60,
            sessionType: SessionType.PersonalTraining);

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, monday, monday, Arg.Any<CancellationToken>())
            .Returns(new[] { session });

        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, monday, monday, _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        var day = result.Value.Days.Single();
        var firstSlot = day.Slots.First();
        firstSlot.Status.Should().Be("BookedByOther");
        firstSlot.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task Handle_CancelledSessionIgnored_SlotAvailable()
    {
        var availability = TrainerAvailability.Create(_trainerId, _gymId);
        availability.AddSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        var monday = new DateOnly(2026, 5, 11);
        var session = Session.Create(
            _gymId, _trainerId, _memberId,
            monday, new TimeOnly(9, 0),
            durationMinutes: 60,
            sessionType: SessionType.PersonalTraining);
        session.Cancel("Changed mind", _memberId, isLateCancellation: false);

        _availabilityRepository
            .GetByTrainerAndGymAsync(_trainerId, _gymId, Arg.Any<CancellationToken>())
            .Returns(availability);
        _sessionRepository
            .GetByTrainerAsync(_trainerId, _gymId, monday, monday, Arg.Any<CancellationToken>())
            .Returns(new[] { session });

        var query = new GetWeeklyAvailabilityQuery(_trainerId, _gymId, monday, monday, _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        var day = result.Value.Days.Single();
        day.Slots.First().Status.Should().Be("Available");
    }

    [Fact]
    public async Task Handle_EndBeforeStart_ReturnsFailure()
    {
        var query = new GetWeeklyAvailabilityQuery(
            _trainerId, _gymId,
            new DateOnly(2026, 5, 11),
            new DateOnly(2026, 5, 10),
            _memberId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidDateRange");
    }
}

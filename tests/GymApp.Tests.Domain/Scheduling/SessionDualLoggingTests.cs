using FluentAssertions;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Events;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Scheduling;

/// <summary>pt.dual_session_logging: coach one-tap log, trainee confirm / dispute, desk reverse.</summary>
public sealed class SessionDualLoggingTests
{
    private static readonly Guid MemberId = Guid.NewGuid();

    private static Session QuickLogged() =>
        Session.LogCompleted(
            new SessionParticipants(Guid.NewGuid(), Guid.NewGuid(), MemberId),
            new SessionSchedule(new DateOnly(2026, 11, 12), new TimeOnly(18, 0), 60),
            notes: "Legs");

    [Fact]
    public void Quick_log_creates_a_completed_session_awaiting_the_trainee()
    {
        var session = QuickLogged();

        session.Status.Should().Be(SessionStatus.Completed);
        session.IsQuickLog.Should().BeTrue();
        session.MemberConfirmation.Should().Be(MemberConfirmationStatus.Pending);
        session.Focus.Should().Be("Legs");
        session.GetDomainEvents().Should().ContainSingle(e => e is SessionCompletedDomainEvent);
    }

    [Fact]
    public void Trainee_confirms_once()
    {
        var session = QuickLogged();

        session.ConfirmByMember(new MemberResponse(MemberId, DateTime.UtcNow)).IsSuccess.Should().BeTrue();
        session.MemberConfirmation.Should().Be(MemberConfirmationStatus.Confirmed);

        session.ConfirmByMember(new MemberResponse(MemberId, DateTime.UtcNow)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Someone_else_cannot_confirm()
    {
        var session = QuickLogged();

        session.ConfirmByMember(new MemberResponse(Guid.NewGuid(), DateTime.UtcNow)).IsFailure.Should().BeTrue();
        session.MemberConfirmation.Should().Be(MemberConfirmationStatus.Pending);
    }

    [Fact]
    public void Dispute_then_desk_reverse_raises_reversed_event()
    {
        var session = QuickLogged();
        session.ClearDomainEvents();

        session.DisputeByMember(new MemberResponse(MemberId, DateTime.UtcNow, "I was not there")).IsSuccess.Should().BeTrue();
        session.DisputeReason.Should().Be("I was not there");
        session.ReverseDisputed().IsSuccess.Should().BeTrue();

        session.MemberConfirmation.Should().Be(MemberConfirmationStatus.Reversed);
        session.GetDomainEvents().Should().ContainSingle(e => e is SessionReversedDomainEvent);
    }

    [Fact]
    public void Only_disputed_sessions_can_be_reversed()
    {
        var session = QuickLogged();

        session.ReverseDisputed().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Booked_session_completed_normally_also_asks_the_trainee_to_confirm()
    {
        var session = Session.Create(
            new SessionParticipants(Guid.NewGuid(), Guid.NewGuid(), MemberId),
            new SessionSchedule(new DateOnly(2030, 1, 1), new TimeOnly(10, 0), 60),
            SessionType.PersonalTraining);
        session.MemberConfirmation.Should().Be(MemberConfirmationStatus.None);

        session.Confirm(new DateTime(2029, 12, 31));
        session.StartSession(new DateTime(2030, 1, 1, 10, 0, 0), new EarlyStartPolicy(15, false));
        session.Complete();

        session.MemberConfirmation.Should().Be(MemberConfirmationStatus.Pending);
    }
}

namespace GymApp.Modules.Scheduling.Domain.Enums;

/// <summary>Trainee side of dual session logging (pt.dual_session_logging).</summary>
public enum MemberConfirmationStatus
{
    /// <summary>Not awaiting the trainee (not completed yet, or logged before dual logging).</summary>
    None = 0,

    /// <summary>Coach logged the session; waiting for the trainee to confirm.</summary>
    Pending = 1,

    Confirmed = 2,

    /// <summary>Trainee said "not me"; the desk reviews it.</summary>
    Disputed = 3,

    /// <summary>Desk reversed a disputed session; the package session was given back.</summary>
    Reversed = 4,
}

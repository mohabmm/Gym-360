using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Errors;

public static class SchedulingErrors
{
    public static Error SessionNotFound(Guid id) =>
        new("Scheduling.SessionNotFound", $"Session with ID '{id}' was not found.");

    public static readonly Error AvailabilityNotFound =
        new("Scheduling.AvailabilityNotFound", "Trainer availability was not found.");

    public static Error AvailabilityNotFoundForTrainer(Guid trainerId) =>
        new("Scheduling.AvailabilityNotFound", $"Trainer availability for trainer '{trainerId}' was not found.");

    public static readonly Error SlotNotAvailable =
        new("Scheduling.SlotNotAvailable", "The requested time slot is not available.");

    public static readonly Error SessionAlreadyBooked =
        new("Scheduling.SessionAlreadyBooked", "A session is already booked for this time slot.");

    public static readonly Error InvalidStatusTransition =
        new("Scheduling.InvalidStatusTransition", "The requested status transition is not valid.");

    public static readonly Error AlreadyConfirmed =
        new("Scheduling.AlreadyConfirmed", "The session is already confirmed.");

    public static Error SessionTooEarly(DateTime scheduledAt, int allowedWindowMinutes) =>
        new("Scheduling.SessionTooEarly",
            $"Session can only be started within {allowedWindowMinutes} minutes of the scheduled time ({scheduledAt:O}). Enable the gym 'early_session_start' feature override to bypass this guard.");

    public static Error SessionInPast(DateTime scheduledAt) =>
        new("Scheduling.SessionInPast",
            $"Cannot confirm a session whose scheduled time ({scheduledAt:O}) is already in the past.");

    public static readonly Error AlreadyCompleted =
        new("Scheduling.AlreadyCompleted", "The session is already completed.");

    public static readonly Error AlreadyCancelled =
        new("Scheduling.AlreadyCancelled", "The session is already cancelled.");

    public static readonly Error TrainerLogAlreadyExists =
        new("Scheduling.TrainerLogAlreadyExists", "A trainer log already exists for this session.");

    public static readonly Error MemberLogAlreadyExists =
        new("Scheduling.MemberLogAlreadyExists", "A member log already exists for this session.");

    public static readonly Error MemberLogRequiresCompletedSession =
        new("Scheduling.MemberLogRequiresCompletedSession", "Feedback can only be submitted after the session is completed.");

    public static readonly Error NotSessionOwner =
        new("Scheduling.NotSessionOwner.Forbidden", "Only the session's member can submit member feedback.");

    public static readonly Error InvalidRating =
        new("Scheduling.InvalidRating", "Rating must be between 1 and 5.");

    public static readonly Error InvalidExertion =
        new("Scheduling.InvalidExertion", "Perceived exertion (RPE) must be between 1 and 10.");

    public static readonly Error DateAlreadyBlocked =
        new("Scheduling.DateAlreadyBlocked", "This date is already blocked.");

    public static readonly Error SlotOverlap =
        new("Scheduling.SlotOverlap", "The new slot overlaps with an existing active slot.");

    public static readonly Error InvalidDateRange =
        new("Scheduling.InvalidDateRange", "The supplied date range is invalid (end before start, or window too large).");

    public static readonly Error ExerciseLogRequiresCompletedSession =
        new("Scheduling.ExerciseLogRequiresCompletedSession", "Exercise logs can only be recorded for a completed session.");

    public static Error ExerciseLogNotFound(Guid id) =>
        new("Scheduling.ExerciseLogNotFound", $"Exercise log with ID '{id}' was not found.");

    public static readonly Error NotSessionTrainer =
        new("Scheduling.NotSessionTrainer.Forbidden", "Only the session's trainer can modify its exercise logs.");

    public static readonly Error NotSessionParticipant =
        new("Scheduling.NotSessionParticipant.Forbidden", "Only the session's trainer or member can read its exercise logs.");
}

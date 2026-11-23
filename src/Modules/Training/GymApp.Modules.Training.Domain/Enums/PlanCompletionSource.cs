namespace GymApp.Modules.Training.Domain.Enums;

/// <summary>Where a finished plan workout came from.</summary>
public enum PlanCompletionSource
{
    /// <summary>The trainee ticked it off in the app (possibly offline, synced later).</summary>
    App = 0,

    /// <summary>The coach logged a PT session and linked it to this plan day.</summary>
    CoachSession = 1
}

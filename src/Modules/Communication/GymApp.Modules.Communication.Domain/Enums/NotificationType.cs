namespace GymApp.Modules.Communication.Domain.Enums;

public enum NotificationType
{
    General = 0,
    SessionReminder = 1,
    TrainerFeedback = 2,
    Announcement = 3,
    MembershipExpiry = 4,
    Achievement = 5,
    WorkoutAssigned = 6,
    Message = 7,
    PaymentReceived = 8,
    StreakMilestone = 9,
    RemoteWorkoutActivated = 10,
    ClassBooked = 11,
    PackageLow = 12,
    CoachOfTheMonth = 13,
    LeaderboardUpdate = 14,
    TrialAlert = 15,
    SessionConfirmation = 16,
    SessionDisputed = 17,

    /// <summary>pt.trial_recap: the coach sent the trainee their post-trial plan.</summary>
    TrialRecap = 18,

    /// <summary>pt.package_interest: a trainee tapped "I'm interested" (desk + coach).</summary>
    PackageInterest = 19,

    /// <summary>pt.trial_followups: post-trial reminders to the trainee.</summary>
    TrialFollowUp = 20,

    /// <summary>pt.training_plan: the coach gave the trainee a plan or changed it.</summary>
    TrainingPlan = 21,

    /// <summary>mem.first_month_challenge: a new member reached the target (trainee, coach, desk).</summary>
    FirstMonthChallenge = 22,

    /// <summary>nutrition: the coach gave the member a diet plan.</summary>
    MealPlan = 23,

    /// <summary>pt.starter_plan: the desk gave a member a package with this coach.</summary>
    NewClientAssigned = 24,

    /// <summary>pt.starter_plan: the staff opened the monthly programs for one of the coach's members.</summary>
    PlanProgramsUnlocked = 25
}

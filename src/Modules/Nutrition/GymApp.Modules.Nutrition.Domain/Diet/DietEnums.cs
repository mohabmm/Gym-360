namespace GymApp.Modules.Nutrition.Domain.Diet;

public enum Sex
{
    Male = 0,
    Female = 1
}

/// <summary>How active the member is outside the gym plus training days, for the calorie multiplier.</summary>
public enum ActivityLevel
{
    /// <summary>Desk job, little walking.</summary>
    Low = 0,

    /// <summary>Trains 1–3 days a week.</summary>
    Light = 1,

    /// <summary>Trains 3–5 days a week.</summary>
    Moderate = 2,

    /// <summary>Trains 6–7 days a week or a physical job.</summary>
    High = 3
}

public enum DietGoal
{
    FatLoss = 0,
    Maintain = 1,
    MuscleGain = 2
}

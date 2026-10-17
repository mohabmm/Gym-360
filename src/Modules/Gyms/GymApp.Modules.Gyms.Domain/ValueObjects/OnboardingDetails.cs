namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>The health and fitness answers a member gives during onboarding.</summary>
public sealed record OnboardingDetails
{
    public string? MedicalHistory { get; init; }
    public string? FitnessGoals { get; init; }
    public string? Injuries { get; init; }
    public string? Medications { get; init; }
    public string? FitnessLevel { get; init; }
}

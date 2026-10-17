using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class MemberOnboarding : Entity
{
    private MemberOnboarding() : base() { }

    private MemberOnboarding(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public string? MedicalHistory { get; private set; }
    public string? FitnessGoals { get; private set; }
    public string? Injuries { get; private set; }
    public string? Medications { get; private set; }
    public string? FitnessLevel { get; private set; }

    public static MemberOnboarding Create(Guid gymId, Guid memberId, OnboardingDetails details)
    {
        var onboarding = new MemberOnboarding(Guid.NewGuid())
        {
            GymId = gymId,
            MemberId = memberId
        };
        onboarding.Update(details);
        return onboarding;
    }

    public void Update(OnboardingDetails details)
    {
        MedicalHistory = details.MedicalHistory;
        FitnessGoals = details.FitnessGoals;
        Injuries = details.Injuries;
        Medications = details.Medications;
        FitnessLevel = details.FitnessLevel;
    }
}

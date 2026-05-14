using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class MemberOnboarding : Entity
{
    private MemberOnboarding() : base() { }

    private MemberOnboarding(
        Guid id,
        Guid gymId,
        Guid memberId,
        string? medicalHistory,
        string? fitnessGoals,
        string? injuries,
        string? medications,
        string? fitnessLevel) : base(id)
    {
        GymId = gymId;
        MemberId = memberId;
        MedicalHistory = medicalHistory;
        FitnessGoals = fitnessGoals;
        Injuries = injuries;
        Medications = medications;
        FitnessLevel = fitnessLevel;
    }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public string? MedicalHistory { get; private set; }
    public string? FitnessGoals { get; private set; }
    public string? Injuries { get; private set; }
    public string? Medications { get; private set; }
    public string? FitnessLevel { get; private set; }

    public static MemberOnboarding Create(
        Guid gymId,
        Guid memberId,
        string? medicalHistory,
        string? fitnessGoals,
        string? injuries,
        string? medications,
        string? fitnessLevel)
    {
        return new MemberOnboarding(
            Guid.NewGuid(),
            gymId,
            memberId,
            medicalHistory,
            fitnessGoals,
            injuries,
            medications,
            fitnessLevel);
    }

    public void Update(
        string? medicalHistory,
        string? fitnessGoals,
        string? injuries,
        string? medications,
        string? fitnessLevel)
    {
        MedicalHistory = medicalHistory;
        FitnessGoals = fitnessGoals;
        Injuries = injuries;
        Medications = medications;
        FitnessLevel = fitnessLevel;
    }
}

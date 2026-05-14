using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.SaveMemberOnboarding;

public sealed record SaveMemberOnboardingCommand(
    Guid GymId,
    Guid MemberId,
    string? MedicalHistory,
    string? FitnessGoals,
    string? Injuries,
    string? Medications,
    string? FitnessLevel) : ICommand<Guid>;

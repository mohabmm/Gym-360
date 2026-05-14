using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.SaveMemberOnboarding;

internal sealed class SaveMemberOnboardingCommandValidator : AbstractValidator<SaveMemberOnboardingCommand>
{
    public SaveMemberOnboardingCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.MedicalHistory).MaximumLength(2000);
        RuleFor(x => x.FitnessGoals).MaximumLength(2000);
        RuleFor(x => x.Injuries).MaximumLength(2000);
        RuleFor(x => x.Medications).MaximumLength(2000);
        RuleFor(x => x.FitnessLevel).MaximumLength(50);
    }
}

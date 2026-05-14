using FluentValidation;

namespace GymApp.Modules.Gamification.Application.Commands.ModerateTestimonial;

internal sealed class ModerateTestimonialCommandValidator : AbstractValidator<ModerateTestimonialCommand>
{
    public ModerateTestimonialCommandValidator()
    {
        RuleFor(x => x.TestimonialId).NotEmpty().WithMessage("Testimonial ID is required.");
        RuleFor(x => x.TrainerId).NotEmpty().WithMessage("Trainer ID is required.");
        RuleFor(x => x.GymId).NotEmpty().WithMessage("Gym ID is required.");
        RuleFor(x => x.ModeratedBy).NotEmpty().WithMessage("Moderator ID is required.");
    }
}

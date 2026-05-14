using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.CreateAnnouncement;

internal sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.ImageUrl).MaximumLength(500).When(x => x.ImageUrl is not null);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.TargetAudience).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow).When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled time must be in the future.");
    }
}

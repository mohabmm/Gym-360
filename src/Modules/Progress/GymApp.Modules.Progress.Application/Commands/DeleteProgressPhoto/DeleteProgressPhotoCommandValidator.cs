using FluentValidation;

namespace GymApp.Modules.Progress.Application.Commands.DeleteProgressPhoto;

internal sealed class DeleteProgressPhotoCommandValidator : AbstractValidator<DeleteProgressPhotoCommand>
{
    public DeleteProgressPhotoCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}

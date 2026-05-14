using FluentValidation;

namespace GymApp.Modules.Progress.Application.Commands.UploadProgressPhoto;

internal sealed class UploadProgressPhotoCommandValidator : AbstractValidator<UploadProgressPhotoCommand>
{
    public UploadProgressPhotoCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.PhotoUrl).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.ThumbnailUrl).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Angle).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}

using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.StartDataImport;

internal sealed class StartDataImportCommandValidator : AbstractValidator<StartDataImportCommand>
{
    private const long MaxFileSizeBytes = 10L * 1024 * 1024;

    public StartDataImportCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.UploadedByUserId).NotEmpty();
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxFileSizeBytes);
        RuleFor(x => x.Content).NotNull();
    }
}

using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Application.Abstractions;

namespace GymApp.Modules.Identity.Application.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        var firstName = request.FirstName ?? user.FirstName;
        var lastName = request.LastName ?? user.LastName;
        var profilePhotoUrl = request.ProfilePhotoUrl ?? user.ProfilePhotoUrl;

        DateTime? dateOfBirth = user.DateOfBirth;
        if (request.DateOfBirth is not null && DateTime.TryParse(request.DateOfBirth, out var parsedDob))
        {
            dateOfBirth = parsedDob;
        }

        var language = user.Language;
        if (request.Language is not null && Enum.TryParse<Language>(request.Language, true, out var parsedLanguage))
        {
            language = parsedLanguage;
        }

        user.UpdateProfile(firstName, lastName, profilePhotoUrl, dateOfBirth, language);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

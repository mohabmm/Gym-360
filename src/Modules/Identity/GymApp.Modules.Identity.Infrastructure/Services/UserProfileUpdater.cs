using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Infrastructure.Services;

internal sealed class UserProfileUpdater : IUserProfileUpdater
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UserProfileUpdater(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> UpdateAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return false;

        if (firstName is not null || lastName is not null)
        {
            user.UpdateProfile(
                firstName ?? user.FirstName,
                lastName ?? user.LastName,
                user.ProfilePhotoUrl,
                user.DateOfBirth,
                user.Language);
        }

        if (phone is not null)
        {
            var phoneResult = PhoneNumber.Create(phone);
            if (phoneResult.IsSuccess)
            {
                user.ChangePhoneNumber(phoneResult.Value);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

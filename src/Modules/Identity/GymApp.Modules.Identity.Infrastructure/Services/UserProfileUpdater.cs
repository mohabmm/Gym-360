using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
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
        UserProfileChanges changes,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return false;

        ApplyNameChanges(user, changes);
        ApplyPhoneChange(user, changes.Phone);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ApplyNameChanges(User user, UserProfileChanges changes)
    {
        if (changes.FirstName is null && changes.LastName is null)
            return;

        user.UpdateProfile(new UserProfile(
            changes.FirstName ?? user.FirstName,
            changes.LastName ?? user.LastName,
            user.ProfilePhotoUrl,
            user.DateOfBirth,
            user.Language));
    }

    // Invalid numbers are ignored (existing behavior).
    private static void ApplyPhoneChange(User user, string? phone)
    {
        if (phone is null)
            return;

        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsSuccess)
            user.ChangePhoneNumber(phoneResult.Value);
    }
}

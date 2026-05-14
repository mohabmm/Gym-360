using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    private readonly IUserRepository _userRepository;
    private readonly IUserConsentRepository _consentRepository;

    public UserDataProvider(
        IUserRepository userRepository,
        IUserConsentRepository consentRepository)
    {
        _userRepository = userRepository;
        _consentRepository = consentRepository;
    }

    public string ModuleName => "Identity";

    public async Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var consents = await _consentRepository.GetByUserIdAsync(userId, cancellationToken);

        if (user is null)
            return new { UserId = userId, Message = "User not found" };

        return new
        {
            Profile = new
            {
                user.Id,
                Email = user.Email.Value,
                PhoneNumber = user.PhoneNumber.Value,
                user.FirstName,
                user.LastName,
                user.DateOfBirth,
                user.Gender,
                user.Role,
                user.Status,
                user.Language,
                user.ProfilePhotoUrl,
                user.CreatedAt
            },
            LinkedGyms = user.GymMemberships.Select(m => new
            {
                m.GymId,
                m.Role,
                m.JoinedAt,
                m.Section,
                m.MemberNumber
            }),
            Consents = consents.Select(c => new
            {
                c.ConsentType,
                c.IsGranted,
                c.GrantedAt,
                c.WithdrawnAt,
                c.IpAddress
            })
        };
    }
}

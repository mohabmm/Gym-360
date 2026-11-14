using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Tests.Application.Identity;

internal static class AdminUserManagementTestData
{
    public static User NewUser(UserRole role = UserRole.Trainee)
    {
        var user = User.Create(
            new NewUserDetails
            {
                Email = Email.Create($"u{Guid.NewGuid():N}@example.com").Value,
                PhoneNumber = PhoneNumber.Create("+201234567890").Value,
                PasswordHash = "hashed",
                FirstName = "Mona",
                LastName = "Adel",
                Gender = Gender.Female
            },
            role);
        user.Activate();
        user.AddRefreshToken(RefreshToken.Create("token", DateTime.UtcNow.AddDays(7)));
        return user;
    }
}

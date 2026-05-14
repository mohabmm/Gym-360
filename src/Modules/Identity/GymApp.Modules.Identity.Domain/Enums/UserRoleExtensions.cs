namespace GymApp.Modules.Identity.Domain.Enums;

public static class UserRoleExtensions
{
    public static string ToRoleClaim(this UserRole role) => role switch
    {
        UserRole.Trainee => "trainee",
        UserRole.Trainer => "trainer",
        UserRole.GymStaff => "staff",
        UserRole.GymOwner => "owner",
        UserRole.PlatformAdmin => "admin",
        _ => role.ToString().ToLowerInvariant()
    };
}

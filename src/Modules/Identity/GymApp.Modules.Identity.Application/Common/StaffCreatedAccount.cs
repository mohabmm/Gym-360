namespace GymApp.Modules.Identity.Application.Common;

/// <summary>
/// Accounts the gym desk creates on someone's behalf (members, coaches).
/// </summary>
public static class StaffCreatedAccount
{
    /// <summary>
    /// The first password of every staff-created account. Staff share it with the person,
    /// who should change it after signing in. Never used to reset an existing account.
    /// </summary>
    public const string DefaultPassword = "12345678";
}

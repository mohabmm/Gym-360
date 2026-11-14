using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Errors;

/// <summary>Errors raised by the platform-admin user management console.</summary>
public static class AdminUserErrors
{
    public static readonly Error CannotDeleteSelf = new(
        "AdminUser.CannotDeleteSelf", "You cannot delete your own account.");

    public static readonly Error CannotChangeOwnStatus = new(
        "AdminUser.CannotChangeOwnStatus", "You cannot suspend or reactivate your own account.");

    public static readonly Error InvalidStatus = new(
        "AdminUser.InvalidStatus", "Status must be 'active' or 'suspended'.");

    public static readonly Error CannotReactivateDeleted = new(
        "AdminUser.CannotReactivateDeleted.Conflict", "A deleted account cannot be reactivated.");

    public static readonly Error CannotDeleteLastSuperAdmin = new(
        "AdminUser.CannotDeleteLastSuperAdmin.Conflict",
        "This is the last super admin. Promote another admin to super admin before deleting this one.");

    public static readonly Error OwnerHasGyms = new(
        "AdminUser.OwnerHasGyms.Conflict",
        "This gym owner still owns gyms. Delete or transfer their gyms first, then delete the owner.");
}

using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class StaffRolePermission : Entity
{
    private StaffRolePermission() : base() { }

    private StaffRolePermission(
        Guid id,
        Guid gymId,
        string staffRole,
        string permissionCode,
        bool isAllowed) : base(id)
    {
        GymId = gymId;
        StaffRole = staffRole;
        PermissionCode = permissionCode;
        IsAllowed = isAllowed;
    }

    public Guid GymId { get; private set; }
    public string StaffRole { get; private set; } = default!;
    public string PermissionCode { get; private set; } = default!;
    public bool IsAllowed { get; private set; }

    public static StaffRolePermission Create(Guid gymId, string staffRole, string permissionCode, bool isAllowed)
    {
        return new StaffRolePermission(Guid.NewGuid(), gymId, staffRole, permissionCode, isAllowed);
    }

    public void SetAllowed(bool isAllowed)
    {
        IsAllowed = isAllowed;
    }
}

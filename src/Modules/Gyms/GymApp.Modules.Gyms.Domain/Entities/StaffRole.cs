using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class StaffRole : Entity
{
    private StaffRole() : base() { }

    private StaffRole(
        Guid id,
        Guid gymId,
        Guid userId,
        StaffRoleType role) : base(id)
    {
        GymId = gymId;
        UserId = userId;
        Role = role;
    }

    public Guid GymId { get; private set; }
    public Guid UserId { get; private set; }
    public StaffRoleType Role { get; private set; }

    public static StaffRole Create(Guid gymId, Guid userId, StaffRoleType role)
    {
        return new StaffRole(Guid.NewGuid(), gymId, userId, role);
    }

    public void UpdateRole(StaffRoleType role)
    {
        Role = role;
    }
}

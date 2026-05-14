using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class PlatformAdmin : Entity
{
    private PlatformAdmin() { }

    public Guid UserId { get; private set; }
    public AdminRole AdminRole { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static PlatformAdmin Create(Guid userId, AdminRole adminRole, Guid? createdBy)
    {
        return new PlatformAdmin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AdminRole = adminRole,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}

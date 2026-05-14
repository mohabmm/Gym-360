namespace GymApp.Modules.Gyms.Domain.Enums;

public enum GymStatus
{
    PendingApproval = 0,
    Active = 1,
    GracePeriod = 2,
    Suspended = 3,
    Terminated = 4,
    Rejected = 5
}

namespace GymApp.Modules.Scheduling.Domain.Enums;

public enum SessionStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    CancelledByTrainer = 4,
    CancelledByMember = 5,
    NoShow = 6,
    LateCancelled = 7
}

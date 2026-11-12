namespace GymApp.Modules.Billing.Domain.Enums;

public enum PackageUsageType
{
    Completed = 0,
    LateCancelled = 1,
    NoShow = 2,

    /// <summary>A previously counted session was reversed (trainee disputed it; desk restored it).</summary>
    Reversed = 3
}

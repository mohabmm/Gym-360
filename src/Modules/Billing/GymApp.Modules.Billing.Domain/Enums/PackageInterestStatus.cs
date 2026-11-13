namespace GymApp.Modules.Billing.Domain.Enums;

/// <summary>
/// Stored state of a package-interest lead. "Converted" is not stored: a lead is converted once
/// the trainee has a paid package created after it (any assign path, including data import).
/// </summary>
public enum PackageInterestStatus
{
    Open = 0,
    Dismissed = 1,
}

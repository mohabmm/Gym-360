namespace GymApp.Common.Infrastructure.Tenancy;

public sealed class TenancyOptions
{
    public TenancyMode Mode { get; init; } = TenancyMode.Soft;
}

public enum TenancyMode
{
    Soft = 0,
    Hard = 1
}

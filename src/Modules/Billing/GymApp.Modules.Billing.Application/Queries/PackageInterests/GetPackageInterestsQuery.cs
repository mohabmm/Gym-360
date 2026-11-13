using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Queries.PackageInterests;

/// <summary>
/// pt.package_interest: the desk's lead list. <see cref="State"/> is one of
/// <see cref="PackageInterestStates"/>; pending leads come first, oldest first, so nobody waits.
/// </summary>
public sealed record GetPackageInterestsQuery(Guid GymId, string State) : IQuery<IReadOnlyList<PackageInterestDto>>;

public sealed record PackageInterestDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string? MemberPhone,
    Guid TrainerId,
    string TrainerName,
    string Source,
    int? RecommendedSessions,
    string State,
    DateTime CreatedAt);

public static class PackageInterestStates
{
    public const string Pending = "pending";
    public const string Converted = "converted";
    public const string Dismissed = "dismissed";
    public const string All = "all";

    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending, Converted, Dismissed, All,
    };
}

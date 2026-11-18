using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Queries.PackageInterests;

/// <summary>
/// pt.package_interest: the trainee's own request, so the app shows "request sent" instead of the
/// "I'm interested" / "Renew" button again after a restart or on another screen.
/// </summary>
public sealed record GetMyPackageInterestQuery(Guid GymId, Guid MemberId) : IQuery<MyPackageInterestDto>;

/// <param name="Pending">True while the desk has not yet followed up (open and not converted).</param>
public sealed record MyPackageInterestDto(bool Pending, Guid? InterestId, Guid? SourcePackageId, DateTime? CreatedAt)
{
    public static readonly MyPackageInterestDto None = new(false, null, null, null);
}

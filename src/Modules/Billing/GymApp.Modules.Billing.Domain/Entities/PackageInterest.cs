using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

/// <summary>
/// pt.package_interest: a lead for the desk. The trainee tapped "I'm interested" after a trial
/// or near the end of a package; the desk follows up and sells the package in person.
/// </summary>
public sealed class PackageInterest : Entity
{
    private PackageInterest() { }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid SourcePackageId { get; private set; }
    public PackageInterestSource Source { get; private set; }
    public int? RecommendedSessions { get; private set; }
    public PackageInterestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DismissedAt { get; private set; }
    public Guid? DismissedBy { get; private set; }

    public static PackageInterest Create(PackageInterestDetails details) => new()
    {
        Id = Guid.NewGuid(),
        GymId = details.GymId,
        MemberId = details.MemberId,
        TrainerId = details.TrainerId,
        SourcePackageId = details.SourcePackageId,
        Source = details.Source,
        RecommendedSessions = details.RecommendedSessions,
        Status = PackageInterestStatus.Open,
        CreatedAt = details.CreatedAtUtc,
    };

    /// <summary>
    /// A lead raised from one of the trainee's packages. Source and size come from the package:
    /// a trial suggests the coach's recommended size (if a recap was sent); a paid package
    /// suggests renewing the same size.
    /// </summary>
    public static PackageInterest ForPackage(SessionPackage package, DateTime nowUtc) => Create(new PackageInterestDetails
    {
        GymId = package.GymId,
        MemberId = package.MemberId,
        TrainerId = package.TrainerId,
        SourcePackageId = package.Id,
        Source = SourceOf(package),
        RecommendedSessions = package.IsTrial ? package.RecommendedSessions : package.TotalSessions,
        CreatedAtUtc = nowUtc,
    });

    private static PackageInterestSource SourceOf(SessionPackage package) => package switch
    {
        { IsTrial: false } => PackageInterestSource.Renewal,
        { RecapSentAt: not null } => PackageInterestSource.TrialRecap,
        _ => PackageInterestSource.TrialFollowUp,
    };

    /// <summary>Converted = the trainee has a paid package created at or after this lead.</summary>
    public bool IsConvertedBy(IEnumerable<DateTime> paidPackageCreatedAts) =>
        paidPackageCreatedAts.Any(created => created >= CreatedAt);

    /// <summary>Still waiting for the desk: stored Open and not yet converted.</summary>
    public bool IsPendingGiven(IEnumerable<DateTime> paidPackageCreatedAts) =>
        Status == PackageInterestStatus.Open && !IsConvertedBy(paidPackageCreatedAts);

    /// <summary>The desk closes a lead that will not convert (not reachable, not interested).</summary>
    public Result Dismiss(Guid staffUserId, DateTime nowUtc)
    {
        if (Status != PackageInterestStatus.Open)
            return Result.Failure(BillingErrors.PackageInterestNotOpen);

        Status = PackageInterestStatus.Dismissed;
        DismissedAt = nowUtc;
        DismissedBy = staffUserId;
        return Result.Success();
    }
}

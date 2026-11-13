using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

/// <summary>
/// pt.package_interest: the trainee taps "I'm interested" on one of their packages (a trial or a
/// low/used-up paid package). Idempotent: while a lead is pending, the same lead is returned.
/// </summary>
public sealed record ExpressPackageInterestCommand(Guid GymId, Guid MemberId, Guid SourcePackageId)
    : ICommand<PackageInterestReceipt>;

/// <param name="AlreadyPending">True when an earlier lead was still pending and no new one was created.</param>
public sealed record PackageInterestReceipt(Guid InterestId, bool AlreadyPending);

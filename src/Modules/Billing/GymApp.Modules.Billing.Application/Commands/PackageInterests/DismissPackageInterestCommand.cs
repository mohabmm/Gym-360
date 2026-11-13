using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

/// <summary>pt.package_interest: the desk closes a lead that will not convert.</summary>
public sealed record DismissPackageInterestCommand(Guid GymId, Guid InterestId, Guid StaffUserId) : ICommand;

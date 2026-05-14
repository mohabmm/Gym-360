using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

public sealed record UseSessionFromPackageCommand(
    Guid PackageId,
    Guid SessionId,
    string UsageType) : ICommand;

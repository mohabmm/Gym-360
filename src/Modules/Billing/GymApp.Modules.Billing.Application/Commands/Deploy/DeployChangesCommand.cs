using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Commands.Deploy;

public sealed record DeployChangesCommand(
    string Scope,
    Guid? TargetId,
    Guid DeployedBy) : ICommand<DeployResult>;

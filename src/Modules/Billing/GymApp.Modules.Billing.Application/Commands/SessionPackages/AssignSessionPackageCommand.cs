using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

public sealed record AssignSessionPackageCommand(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    Guid TemplateId,
    Guid AssignedBy,
    DateTime? ExpiresAt) : ICommand<Guid>;

using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.PackageTemplates;

public sealed record CreatePackageTemplateCommand(
    Guid GymId,
    string Name,
    int SessionCount,
    decimal PricePerSession) : ICommand<Guid>;

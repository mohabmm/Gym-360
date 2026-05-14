using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.PackageTemplates;

public sealed record UpdatePackageTemplateCommand(
    Guid TemplateId,
    string? Name,
    int? SessionCount,
    decimal? PricePerSession,
    bool? IsActive) : ICommand;

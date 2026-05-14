using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Features;

public sealed record CreateFeatureCommand(
    string Code,
    string Name,
    string? Description,
    string? Category) : ICommand<Guid>;

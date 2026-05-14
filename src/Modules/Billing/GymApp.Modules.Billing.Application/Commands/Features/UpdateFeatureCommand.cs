using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Features;

public sealed record UpdateFeatureCommand(
    Guid FeatureId,
    string? Name,
    string? Description,
    string? Category,
    bool? IsActive) : ICommand;

using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;

public sealed record SetGymFeatureOverrideCommand(
    Guid GymId,
    Guid FeatureId,
    string? FeatureCode,
    bool Enabled,
    string? Reason,
    Guid SetBy) : ICommand;

using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;

public sealed record RemoveGymFeatureOverrideCommand(
    Guid GymId,
    Guid FeatureId) : ICommand;

using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.PlatformSettings;

public sealed record UpdatePlatformSettingsCommand(
    Dictionary<string, string?> Values,
    Guid? UpdatedBy) : ICommand;

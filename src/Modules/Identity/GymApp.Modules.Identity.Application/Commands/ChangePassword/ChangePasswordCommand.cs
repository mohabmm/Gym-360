using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand;

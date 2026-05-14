using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand;

using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;

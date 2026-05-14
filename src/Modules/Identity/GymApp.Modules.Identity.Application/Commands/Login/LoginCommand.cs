using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<TokenResponse>;

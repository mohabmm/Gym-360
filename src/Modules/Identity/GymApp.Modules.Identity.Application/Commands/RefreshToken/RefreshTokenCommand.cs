using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token) : ICommand<TokenResponse>;

using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Commands.SelectGym;

public sealed record SelectGymCommand(Guid UserId, Guid GymId) : ICommand<TokenResponse>;

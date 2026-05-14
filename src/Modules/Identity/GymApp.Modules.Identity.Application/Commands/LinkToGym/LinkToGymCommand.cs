using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.LinkToGym;

public sealed record LinkToGymCommand(Guid UserId, string GymCode) : ICommand;

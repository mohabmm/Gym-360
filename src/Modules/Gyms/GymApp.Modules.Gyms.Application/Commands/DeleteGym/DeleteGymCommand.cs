using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.DeleteGym;

public sealed record DeleteGymCommand(Guid GymId) : ICommand;

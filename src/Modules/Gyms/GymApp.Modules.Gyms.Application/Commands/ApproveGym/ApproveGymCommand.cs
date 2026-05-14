using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveGym;

public sealed record ApproveGymCommand(Guid GymId) : ICommand;

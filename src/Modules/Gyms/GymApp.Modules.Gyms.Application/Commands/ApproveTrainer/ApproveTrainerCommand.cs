using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveTrainer;

public sealed record ApproveTrainerCommand(Guid GymId, Guid TrainerId) : ICommand;

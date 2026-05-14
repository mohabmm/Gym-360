using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.RejectTrainer;

public sealed record RejectTrainerCommand(Guid GymId, Guid TrainerId) : ICommand;

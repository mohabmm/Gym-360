using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.Commands.AssignProgram;

public sealed record AssignProgramCommand(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    Guid ProgramId,
    DateTime StartDate,
    string? Notes) : ICommand<Guid>;

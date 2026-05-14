using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Commands.CreateProgram;

public sealed record CreateProgramCommand(
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? NameAr,
    string? Description,
    int DurationWeeks,
    List<ProgramWeekDto> Weeks,
    bool IsTemplate) : ICommand<Guid>;

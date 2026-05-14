using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetPrograms;

public sealed record GetProgramsQuery(
    Guid TrainerId,
    Guid GymId) : IQuery<List<ProgramDto>>;

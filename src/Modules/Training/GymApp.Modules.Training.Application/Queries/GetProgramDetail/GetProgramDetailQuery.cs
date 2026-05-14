using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetProgramDetail;

public sealed record GetProgramDetailQuery(Guid ProgramId) : IQuery<ProgramDetailDto>;

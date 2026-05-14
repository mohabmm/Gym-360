using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetTemplates;

public sealed record GetTemplatesQuery(Guid GymId) : IQuery<List<WorkoutDto>>;

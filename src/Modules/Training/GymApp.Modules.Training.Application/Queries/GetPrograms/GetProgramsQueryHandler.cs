using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetPrograms;

internal sealed class GetProgramsQueryHandler : IQueryHandler<GetProgramsQuery, List<ProgramDto>>
{
    private readonly IProgramRepository _programRepository;

    public GetProgramsQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<Result<List<ProgramDto>>> Handle(
        GetProgramsQuery request, CancellationToken cancellationToken)
    {
        var programs = await _programRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        var dtos = programs.Select(p => new ProgramDto(
            p.Id,
            p.TrainerId,
            p.GymId,
            p.Name,
            p.NameAr,
            p.Description,
            p.DurationWeeks,
            p.IsTemplate,
            p.ProgramWeeks.Count,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        return Result.Success(dtos);
    }
}

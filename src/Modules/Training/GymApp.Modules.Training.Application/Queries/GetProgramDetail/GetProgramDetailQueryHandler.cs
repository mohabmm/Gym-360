using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetProgramDetail;

internal sealed class GetProgramDetailQueryHandler : IQueryHandler<GetProgramDetailQuery, ProgramDetailDto>
{
    private readonly IProgramRepository _programRepository;

    public GetProgramDetailQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<Result<ProgramDetailDto>> Handle(
        GetProgramDetailQuery request, CancellationToken cancellationToken)
    {
        var program = await _programRepository.GetByIdAsync(request.ProgramId, cancellationToken);

        if (program is null)
        {
            return Result.Failure<ProgramDetailDto>(TrainingErrors.ProgramNotFound);
        }

        var weeks = program.ProgramWeeks
            .OrderBy(w => w.WeekNumber)
            .Select(w => new ProgramWeekDto(
                w.WeekNumber,
                w.Theme,
                w.DayWorkouts.Select(d => new ProgramDayWorkoutDto(
                    d.DayOfWeek.ToString(),
                    d.WorkoutId)).ToList()))
            .ToList();

        var dto = new ProgramDetailDto(
            program.Id,
            program.TrainerId,
            program.GymId,
            program.Name,
            program.NameAr,
            program.Description,
            program.DurationWeeks,
            program.IsTemplate,
            weeks,
            program.CreatedAt,
            program.UpdatedAt);

        return Result.Success(dto);
    }
}

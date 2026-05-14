using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetMyWorkoutAssignments;

internal sealed class GetMyWorkoutAssignmentsQueryHandler
    : IQueryHandler<GetMyWorkoutAssignmentsQuery, List<WorkoutAssignmentDto>>
{
    private readonly IWorkoutAssignmentRepository _assignmentRepository;

    public GetMyWorkoutAssignmentsQueryHandler(IWorkoutAssignmentRepository assignmentRepository)
    {
        _assignmentRepository = assignmentRepository;
    }

    public async Task<Result<List<WorkoutAssignmentDto>>> Handle(
        GetMyWorkoutAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var assignments = await _assignmentRepository.GetByClientAsync(
            request.ClientId, request.GymId, cancellationToken);

        var dtos = assignments.Select(a => new WorkoutAssignmentDto(
            a.Id,
            a.TrainerId,
            a.ClientId,
            a.GymId,
            a.WorkoutId,
            a.ProgramId,
            a.AssignmentType.ToString(),
            a.StartDate,
            a.EndDate,
            a.Status.ToString(),
            a.Notes,
            a.IsRemoteMode)).ToList();

        return Result.Success(dtos);
    }
}

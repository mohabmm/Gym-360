using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.AssignProgram;

internal sealed class AssignProgramCommandHandler : ICommandHandler<AssignProgramCommand, Guid>
{
    private readonly IWorkoutAssignmentRepository _assignmentRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public AssignProgramCommandHandler(
        IWorkoutAssignmentRepository assignmentRepository,
        IProgramRepository programRepository,
        ITrainingUnitOfWork unitOfWork)
    {
        _assignmentRepository = assignmentRepository;
        _programRepository = programRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AssignProgramCommand request, CancellationToken cancellationToken)
    {
        var program = await _programRepository.GetByIdAsync(request.ProgramId, cancellationToken);

        if (program is null)
        {
            return Result.Failure<Guid>(TrainingErrors.ProgramNotFound);
        }

        var endDate = DateOnly.FromDateTime(request.StartDate).AddDays(program.DurationWeeks * 7);

        var assignment = WorkoutAssignment.Create(
            request.TrainerId,
            request.ClientId,
            request.GymId,
            AssignmentType.Program,
            DateOnly.FromDateTime(request.StartDate),
            programId: request.ProgramId,
            endDate: endDate,
            notes: request.Notes);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(assignment.Id);
    }
}

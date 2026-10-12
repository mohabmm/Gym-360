using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Commands.UpdateSessionExerciseLog;

internal sealed class UpdateSessionExerciseLogCommandHandler : ICommandHandler<UpdateSessionExerciseLogCommand>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public UpdateSessionExerciseLogCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSessionExerciseLogCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        if (session.TrainerId != request.TrainerId)
            return Result.Failure(SchedulingErrors.NotSessionTrainer);

        var result = session.UpdateExerciseLog(
            request.LogId,
            new ExerciseLogDetails
            {
                ExerciseId = request.ExerciseId,
                ExerciseName = request.ExerciseName,
                ExerciseNameAr = request.ExerciseNameAr,
                OrderIndex = request.OrderIndex,
                Sets = request.Sets,
                Reps = request.Reps,
                Weight = request.Weight,
                Notes = request.Notes
            });

        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

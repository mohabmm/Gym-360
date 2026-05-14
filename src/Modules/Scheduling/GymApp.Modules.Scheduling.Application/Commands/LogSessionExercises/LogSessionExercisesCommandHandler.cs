using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;

internal sealed class LogSessionExercisesCommandHandler
    : ICommandHandler<LogSessionExercisesCommand, IReadOnlyList<SessionExerciseLogDto>>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public LogSessionExercisesCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<SessionExerciseLogDto>>> Handle(
        LogSessionExercisesCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(
                SchedulingErrors.SessionNotFound(request.SessionId));

        if (session.TrainerId != request.TrainerId)
            return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(SchedulingErrors.NotSessionTrainer);

        var created = new List<SessionExerciseLog>();
        foreach (var item in request.Items)
        {
            var addResult = session.AddExerciseLog(
                item.ExerciseId,
                item.ExerciseName,
                item.ExerciseNameAr,
                item.OrderIndex,
                item.Sets,
                item.Reps,
                item.Weight,
                item.Notes);

            if (addResult.IsFailure)
                return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(addResult.Error);

            created.Add(addResult.Value);
        }

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        IReadOnlyList<SessionExerciseLogDto> dtos = created
            .OrderBy(l => l.OrderIndex)
            .Select(l => new SessionExerciseLogDto(
                l.Id, l.SessionId, l.ExerciseId, l.ExerciseName, l.ExerciseNameAr,
                l.OrderIndex, l.Sets, l.Reps, l.Weight, l.Notes, l.CreatedAt, l.UpdatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}

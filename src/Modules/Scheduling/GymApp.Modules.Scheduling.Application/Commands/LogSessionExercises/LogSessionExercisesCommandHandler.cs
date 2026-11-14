using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

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

        var labelled = request.DayLabel is null ? Result.Success() : session.SetDayLabel(request.DayLabel);
        if (labelled.IsFailure)
            return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(labelled.Error);

        var created = new List<SessionExerciseLog>();
        foreach (var item in request.Items)
        {
            var addResult = session.AddExerciseLog(new ExerciseLogDetails
            {
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName,
                ExerciseNameAr = item.ExerciseNameAr,
                OrderIndex = item.OrderIndex,
                Sets = item.Sets,
                Reps = item.Reps,
                Weight = item.Weight,
                Notes = item.Notes,
                Target = item.Target
            });

            if (addResult.IsFailure)
                return Result.Failure<IReadOnlyList<SessionExerciseLogDto>>(addResult.Error);

            created.Add(addResult.Value);
        }

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        IReadOnlyList<SessionExerciseLogDto> dtos = created
            .OrderBy(l => l.OrderIndex)
            .Select(SessionExerciseLogMapper.ToDto)
            .ToList();

        return Result.Success(dtos);
    }
}

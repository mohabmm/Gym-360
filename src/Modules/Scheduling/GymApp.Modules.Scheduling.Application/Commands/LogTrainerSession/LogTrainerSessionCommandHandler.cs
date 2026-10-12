using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.LogTrainerSession;

internal sealed class LogTrainerSessionCommandHandler : ICommandHandler<LogTrainerSessionCommand>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public LogTrainerSessionCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogTrainerSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        var result = session.LogTrainerSession(new TrainerLogDetails
        {
            ExercisesPerformed = request.ExercisesPerformed,
            ActualVsPlanned = request.ActualVsPlanned,
            ClientEffortRating = request.ClientEffortRating,
            Feedback = request.Feedback,
            TechniqueNotes = request.TechniqueNotes,
            AreasToImprove = request.AreasToImprove,
            NextSessionFocus = request.NextSessionFocus,
            AttendanceConfirmed = request.AttendanceConfirmed
        });

        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

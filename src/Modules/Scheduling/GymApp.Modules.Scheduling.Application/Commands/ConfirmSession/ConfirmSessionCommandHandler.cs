using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.ConfirmSession;

internal sealed class ConfirmSessionCommandHandler : ICommandHandler<ConfirmSessionCommand>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmSessionCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ConfirmSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        // Scheduled time is gym-local wall-clock; compare against gym-local "now".
        var result = session.Confirm(GymLocalTime.FromUtc(_dateTimeProvider.UtcNow));
        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

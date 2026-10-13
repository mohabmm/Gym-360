using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.LogMemberSession;

internal sealed class LogMemberSessionCommandHandler : ICommandHandler<LogMemberSessionCommand>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public LogMemberSessionCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogMemberSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        if (session.MemberId != request.MemberId)
            return Result.Failure(SchedulingErrors.NotSessionOwner);

        var result = session.LogMemberSession(new MemberLogDetails
        {
            Rating = request.Rating,
            PerceivedExertion = request.PerceivedExertion,
            PersonalNotes = request.PersonalNotes,
            Discomfort = request.Discomfort,
            Satisfaction = request.Satisfaction
        });

        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

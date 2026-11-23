using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.ValueObjects;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.IntegrationEvents;

namespace GymApp.Modules.Training.Application.TrainingPlans.Coaching;

/// <summary>
/// A coach's push reminder to one client, at most once a (gym-local) day per client so a
/// trainee is never spammed. The push goes out through Communication.
/// </summary>
internal sealed class NudgeTraineeCommandHandler : ICommandHandler<NudgeTraineeCommand, NudgeResultDto>
{
    public const int MaxMessageLength = 300;

    private readonly PlanAccess _access;
    private readonly ICacheService _cache;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public NudgeTraineeCommandHandler(
        PlanAccess access, ICacheService cache, IIntegrationEventPublisher integrationEvents, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _cache = cache;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<NudgeResultDto>> Handle(NudgeTraineeCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<NudgeResultDto>(enabled.Error);
        if (!await _access.IsCoachOfAsync(request.Actor, request.TraineeId, cancellationToken))
            return PlanAccess.Forbidden<NudgeResultDto>();
        var message = request.Message.OrNull();
        if (message is { Length: > MaxMessageLength })
            return Result.Failure<NudgeResultDto>(TrainingPlanErrors.InvalidSettings($"The message is at most {MaxMessageLength} characters."));

        var key = $"training-plan-nudge:{request.Actor.GymId}:{request.TraineeId}:{TrainingCalendar.Today():yyyy-MM-dd}";
        if (await _cache.GetAsync<DateTime?>(key, cancellationToken) is not null)
            return Result.Failure<NudgeResultDto>(TrainingPlanErrors.AlreadyNudged);

        var sentAt = DateTime.UtcNow;
        _integrationEvents.Enqueue(new TrainingPlanNudgeIntegrationEvent(request.Actor.GymId, request.Actor.UserId, request.TraineeId, message));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.SetAsync<DateTime?>(key, sentAt, TimeSpan.FromHours(26), cancellationToken);
        return Result.Success(new NudgeResultDto(sentAt));
    }
}

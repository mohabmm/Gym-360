using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;
using GymApp.Modules.Training.IntegrationEvents;

namespace GymApp.Modules.Training.Application.TrainingPlans.Starter;

/// <summary>The staff open or close the monthly programs for one coach–member pair; opening tells the coach.</summary>
internal sealed class SetProgramUnlockCommandHandler : ICommandHandler<SetProgramUnlockCommand, ProgramUnlockDto>
{
    private readonly PlanAccess _access;
    private readonly IPlanProgramUnlockRepository _unlocks;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public SetProgramUnlockCommandHandler(
        PlanAccess access,
        IPlanProgramUnlockRepository unlocks,
        IIntegrationEventPublisher integrationEvents,
        ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _unlocks = unlocks;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProgramUnlockDto>> Handle(SetProgramUnlockCommand request, CancellationToken cancellationToken)
    {
        var pair = request.Pair;
        var enabled = await _access.EnsureStarterPlanEnabledAsync(pair.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<ProgramUnlockDto>(enabled.Error);
        if (request.Unlocked && !await _access.HasPackageAsync(pair, cancellationToken))
            return Result.Failure<ProgramUnlockDto>(TrainingPlanErrors.UnlockNotYourClient);

        var unlock = await _unlocks.GetAsync(pair, cancellationToken);
        var change = request.Unlocked
            ? await OpenAsync(unlock, request, cancellationToken)
            : Close(unlock, request.StaffUserId);
        if (change.Opened)
            _integrationEvents.Enqueue(new PlanProgramsUnlockedIntegrationEvent(
                pair.GymId, pair.TrainerId, pair.TraineeId, request.StaffUserId, PlanLimitRules.LengthDays(PlanLimitRules.For(request.Length))));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(StarterStatus.Row(pair.TrainerId, string.Empty, change.Unlock, active: null));
    }

    private sealed record UnlockChange(PlanProgramUnlock? Unlock, bool Opened);

    // Closing a pair that was never opened is a no-op.
    private static UnlockChange Close(PlanProgramUnlock? unlock, Guid by)
    {
        unlock?.Lock(by);
        return new UnlockChange(unlock, Opened: false);
    }

    // Opened is true when this call opened a program or changed its length (the coach is told again).
    private async Task<UnlockChange> OpenAsync(PlanProgramUnlock? unlock, SetProgramUnlockCommand request, CancellationToken cancellationToken)
    {
        if (unlock is not null)
            return new UnlockChange(unlock, unlock.Unlock(request.Length, request.StaffUserId));
        var granted = PlanProgramUnlock.Grant(request.Pair, request.Length, request.StaffUserId);
        await _unlocks.AddAsync(granted, cancellationToken);
        return new UnlockChange(granted, Opened: true);
    }
}

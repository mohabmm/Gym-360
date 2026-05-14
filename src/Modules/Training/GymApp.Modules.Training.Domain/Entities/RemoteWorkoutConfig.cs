using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Events;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class RemoteWorkoutConfig : Entity
{
    private RemoteWorkoutConfig() { }
    private RemoteWorkoutConfig(Guid id) : base(id) { }

    public Guid TrainerId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid GymId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public string? Reason { get; private set; }

    public static RemoteWorkoutConfig Create(
        Guid trainerId,
        Guid clientId,
        Guid gymId,
        string? reason = null)
    {
        var config = new RemoteWorkoutConfig(Guid.NewGuid())
        {
            TrainerId = trainerId,
            ClientId = clientId,
            GymId = gymId,
            IsActive = true,
            ActivatedAt = DateTime.UtcNow,
            Reason = reason
        };

        config.RaiseDomainEvent(new RemoteWorkoutActivatedDomainEvent(trainerId, clientId, gymId));

        return config;
    }

    public Result Activate(string? reason = null)
    {
        if (IsActive)
            return Result.Failure(Errors.TrainingErrors.RemoteWorkoutAlreadyActive);

        IsActive = true;
        ActivatedAt = DateTime.UtcNow;
        DeactivatedAt = null;
        Reason = reason;

        RaiseDomainEvent(new RemoteWorkoutActivatedDomainEvent(TrainerId, ClientId, GymId));

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(Errors.TrainingErrors.RemoteWorkoutNotActive);

        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new RemoteWorkoutDeactivatedDomainEvent(TrainerId, ClientId, GymId));

        return Result.Success();
    }
}

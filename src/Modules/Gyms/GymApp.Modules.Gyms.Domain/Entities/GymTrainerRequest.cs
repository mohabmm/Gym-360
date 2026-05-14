using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymTrainerRequest : Entity
{
    private readonly List<string> _specializations = new();

    private GymTrainerRequest() : base() { }

    private GymTrainerRequest(Guid id, Guid gymId, Guid trainerId, TrainerRequestStatus status)
        : base(id)
    {
        GymId = gymId;
        TrainerId = trainerId;
        Status = status;
        RequestedAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public TrainerRequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? Bio { get; private set; }
    public IReadOnlyList<string> Specializations => _specializations.AsReadOnly();

    public void UpdateProfile(IEnumerable<string>? specializations, string? bio)
    {
        if (specializations is not null)
        {
            _specializations.Clear();
            foreach (var s in specializations
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _specializations.Add(s!);
            }
        }
        if (bio is not null)
        {
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        }
    }

    public static GymTrainerRequest CreatePending(Guid gymId, Guid trainerId)
        => new(Guid.NewGuid(), gymId, trainerId, TrainerRequestStatus.Pending);

    public static GymTrainerRequest CreateApproved(Guid gymId, Guid trainerId)
        => new(Guid.NewGuid(), gymId, trainerId, TrainerRequestStatus.Approved)
        {
            DecidedAt = DateTime.UtcNow
        };

    public static GymTrainerRequest CreateRejected(Guid gymId, Guid trainerId)
        => new(Guid.NewGuid(), gymId, trainerId, TrainerRequestStatus.Rejected)
        {
            DecidedAt = DateTime.UtcNow
        };

    public Result Approve()
    {
        if (Status == TrainerRequestStatus.Approved)
            return Result.Success();

        if (Status == TrainerRequestStatus.Rejected)
            return Result.Failure(GymErrors.TrainerAlreadyRejected);

        Status = TrainerRequestStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TrainerApprovedDomainEvent(GymId, TrainerId));
        return Result.Success();
    }

    public Result Reject()
    {
        if (Status == TrainerRequestStatus.Rejected)
            return Result.Success();

        Status = TrainerRequestStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TrainerRejectedDomainEvent(GymId, TrainerId));
        return Result.Success();
    }
}

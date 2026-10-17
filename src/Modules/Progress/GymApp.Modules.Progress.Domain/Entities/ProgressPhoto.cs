using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.DomainEvents;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Domain.Entities;

public sealed class ProgressPhoto : Entity
{
    private ProgressPhoto() { }

    private ProgressPhoto(Guid id) : base(id) { }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public string PhotoUrl { get; private set; } = default!;
    public string ThumbnailUrl { get; private set; } = default!;
    public PhotoAngle Angle { get; private set; }
    public DateOnly Date { get; private set; }
    public bool IsSharedWithTrainer { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<ProgressPhoto> Create(Guid userId, Guid gymId, ProgressPhotoDetails details)
    {
        var photo = new ProgressPhoto(Guid.NewGuid())
        {
            UserId = userId,
            GymId = gymId,
            PhotoUrl = details.PhotoUrl,
            ThumbnailUrl = details.ThumbnailUrl,
            Angle = details.Angle,
            Date = details.Date,
            IsSharedWithTrainer = details.IsSharedWithTrainer,
            Notes = details.Notes,
            CreatedAt = DateTime.UtcNow
        };

        photo.RaiseDomainEvent(new ProgressPhotoUploadedDomainEvent(photo.Id, userId));

        return Result.Success(photo);
    }

    public void ShareWithTrainer()
    {
        IsSharedWithTrainer = true;
    }

    public void UnshareWithTrainer()
    {
        IsSharedWithTrainer = false;
    }
}

using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.DomainEvents;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.Entities;

public sealed class ProgressPhoto : Entity
{
    private ProgressPhoto() { }

    private ProgressPhoto(
        Guid id,
        Guid userId,
        Guid gymId,
        string photoUrl,
        string thumbnailUrl,
        PhotoAngle angle,
        DateOnly date,
        bool isSharedWithTrainer,
        string? notes)
        : base(id)
    {
        UserId = userId;
        GymId = gymId;
        PhotoUrl = photoUrl;
        ThumbnailUrl = thumbnailUrl;
        Angle = angle;
        Date = date;
        IsSharedWithTrainer = isSharedWithTrainer;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public string PhotoUrl { get; private set; } = default!;
    public string ThumbnailUrl { get; private set; } = default!;
    public PhotoAngle Angle { get; private set; }
    public DateOnly Date { get; private set; }
    public bool IsSharedWithTrainer { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<ProgressPhoto> Create(
        Guid userId,
        Guid gymId,
        string photoUrl,
        string thumbnailUrl,
        PhotoAngle angle,
        DateOnly date,
        bool isSharedWithTrainer = false,
        string? notes = null)
    {
        var photo = new ProgressPhoto(
            Guid.NewGuid(),
            userId,
            gymId,
            photoUrl,
            thumbnailUrl,
            angle,
            date,
            isSharedWithTrainer,
            notes);

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

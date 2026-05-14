using GymApp.Common.Domain;

namespace GymApp.Modules.Progress.Domain.DomainEvents;

public sealed record ProgressPhotoUploadedDomainEvent(Guid PhotoId, Guid UserId) : IDomainEvent;

using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Progress.Application.Commands.UploadProgressPhoto;

public sealed record UploadProgressPhotoCommand(
    Guid UserId,
    Guid GymId,
    string PhotoUrl,
    string ThumbnailUrl,
    string Angle,
    DateOnly Date,
    string? Notes) : ICommand<Guid>;

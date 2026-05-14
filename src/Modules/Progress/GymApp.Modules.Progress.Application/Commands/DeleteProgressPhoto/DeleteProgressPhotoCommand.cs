using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Progress.Application.Commands.DeleteProgressPhoto;

public sealed record DeleteProgressPhotoCommand(
    Guid UserId,
    Guid PhotoId) : ICommand;

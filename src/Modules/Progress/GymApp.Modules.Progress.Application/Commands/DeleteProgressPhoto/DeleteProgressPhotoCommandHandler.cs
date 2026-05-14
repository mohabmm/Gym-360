using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Application.Abstractions;

namespace GymApp.Modules.Progress.Application.Commands.DeleteProgressPhoto;

internal sealed class DeleteProgressPhotoCommandHandler : ICommandHandler<DeleteProgressPhotoCommand>
{
    private readonly IProgressPhotoRepository _repository;
    private readonly IProgressUnitOfWork _unitOfWork;

    public DeleteProgressPhotoCommandHandler(
        IProgressPhotoRepository repository,
        IProgressUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProgressPhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await _repository.GetByIdAsync(request.PhotoId, cancellationToken);

        if (photo is null)
        {
            return Result.Failure(ProgressErrors.PhotoNotFound);
        }

        if (photo.UserId != request.UserId)
        {
            return Result.Failure(ProgressErrors.PhotoNotFound);
        }

        _repository.Delete(photo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

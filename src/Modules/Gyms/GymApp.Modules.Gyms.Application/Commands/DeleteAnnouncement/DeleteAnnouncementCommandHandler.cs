using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.DeleteAnnouncement;

internal sealed class DeleteAnnouncementCommandHandler : ICommandHandler<DeleteAnnouncementCommand>
{
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public DeleteAnnouncementCommandHandler(
        IAnnouncementRepository announcementRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _announcementRepository = announcementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _announcementRepository.GetByIdAsync(request.AnnouncementId, cancellationToken);
        if (announcement is null || announcement.GymId != request.GymId)
            return Result.Failure(GymErrors.AnnouncementNotFound);

        _announcementRepository.Remove(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

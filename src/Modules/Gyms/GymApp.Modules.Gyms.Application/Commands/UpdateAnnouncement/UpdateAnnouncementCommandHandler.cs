using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateAnnouncement;

internal sealed class UpdateAnnouncementCommandHandler : ICommandHandler<UpdateAnnouncementCommand>
{
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateAnnouncementCommandHandler(
        IAnnouncementRepository announcementRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _announcementRepository = announcementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _announcementRepository.GetByIdAsync(request.AnnouncementId, cancellationToken);
        if (announcement is null || announcement.GymId != request.GymId)
            return Result.Failure(GymErrors.AnnouncementNotFound);

        // Content edit: only when a title is supplied (the toggle sends IsActive only).
        if (request.Title is not null)
        {
            var category = announcement.Category;
            if (request.Category is not null && Enum.TryParse<AnnouncementCategory>(request.Category, ignoreCase: true, out var parsedCategory))
                category = parsedCategory;

            var audience = announcement.TargetAudience;
            if (request.TargetAudience is not null && Enum.TryParse<TargetAudience>(request.TargetAudience, ignoreCase: true, out var parsedAudience))
                audience = parsedAudience;

            announcement.Update(
                request.Title,
                request.Content ?? announcement.Content,
                announcement.ImageUrl,
                category,
                audience);
        }

        // Publish/unpublish toggle.
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                if (announcement.PublishedAt is null)
                    announcement.Publish();
            }
            else
            {
                announcement.Unpublish();
            }
        }

        _announcementRepository.Update(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

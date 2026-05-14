using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.CreateAnnouncement;

internal sealed class CreateAnnouncementCommandHandler : ICommandHandler<CreateAnnouncementCommand, Guid>
{
    private readonly IGymRepository _gymRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public CreateAnnouncementCommandHandler(
        IGymRepository gymRepository,
        IAnnouncementRepository announcementRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _announcementRepository = announcementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<Guid>(GymErrors.NotFound);

        if (!Enum.TryParse<AnnouncementCategory>(request.Category, ignoreCase: true, out var category))
            category = AnnouncementCategory.General;

        if (!Enum.TryParse<TargetAudience>(request.TargetAudience, ignoreCase: true, out var audience))
            audience = TargetAudience.All;

        var announcement = Announcement.Create(
            request.GymId,
            request.Title,
            request.Content,
            request.ImageUrl,
            category,
            audience,
            request.ScheduledAt,
            request.CreatedBy);

        // Publish immediately if not scheduled
        if (request.ScheduledAt is null)
            announcement.Publish();

        await _announcementRepository.AddAsync(announcement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(announcement.Id);
    }
}

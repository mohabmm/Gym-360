using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

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
            announcement.Update(BuildDetails(announcement, request.Title, request));

        ApplyPublishToggle(announcement, request.IsActive);

        _announcementRepository.Update(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static AnnouncementDetails BuildDetails(Announcement announcement, string title, UpdateAnnouncementCommand request) => new()
    {
        Title = title,
        Content = request.Content ?? announcement.Content,
        ImageUrl = announcement.ImageUrl,
        Category = ParseOrKeep(request.Category, announcement.Category),
        TargetAudience = ParseOrKeep(request.TargetAudience, announcement.TargetAudience)
    };

    private static TEnum ParseOrKeep<TEnum>(string? value, TEnum current) where TEnum : struct, Enum =>
        value is not null && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : current;

    // Publish/unpublish toggle.
    private static void ApplyPublishToggle(Announcement announcement, bool? isActive)
    {
        if (isActive is null)
            return;

        if (!isActive.Value)
            announcement.Unpublish();
        else if (announcement.PublishedAt is null)
            announcement.Publish();
    }
}

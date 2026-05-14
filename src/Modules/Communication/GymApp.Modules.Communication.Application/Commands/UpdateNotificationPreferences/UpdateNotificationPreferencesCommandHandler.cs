using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.Commands.UpdateNotificationPreferences;

internal sealed class UpdateNotificationPreferencesCommandHandler : ICommandHandler<UpdateNotificationPreferencesCommand>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public UpdateNotificationPreferencesCommandHandler(
        INotificationPreferenceRepository preferenceRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _preferenceRepository = preferenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var preference = await _preferenceRepository.GetByUserAsync(request.UserId, request.GymId, cancellationToken);
        var isNew = preference is null;

        preference ??= NotificationPreference.Create(request.UserId, request.GymId);

        foreach (var pref in request.Preferences)
        {
            if (Enum.TryParse<NotificationType>(pref.NotificationType, ignoreCase: true, out var notificationType))
            {
                preference.SetPreference(notificationType, pref.PushEnabled, pref.InAppEnabled);
            }
        }

        preference.SetQuietHours(request.QuietHoursStart, request.QuietHoursEnd);

        if (isNew)
        {
            await _preferenceRepository.AddAsync(preference, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Queries.GetNotificationPreferences;

internal sealed class GetNotificationPreferencesQueryHandler
    : IQueryHandler<GetNotificationPreferencesQuery, NotificationPreferencesResponse>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;

    public GetNotificationPreferencesQueryHandler(INotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    public async Task<Result<NotificationPreferencesResponse>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var preference = await _preferenceRepository.GetByUserAsync(
            request.UserId, request.GymId, cancellationToken);

        if (preference is null)
        {
            // Return defaults — all enabled, no quiet hours
            preference = NotificationPreference.Create(request.UserId, request.GymId);
        }

        var preferenceDtos = preference.Preferences.Select(p =>
            new NotificationPreferenceDto(
                p.NotificationType.ToString(),
                p.PushEnabled,
                p.InAppEnabled)).ToList();

        var response = new NotificationPreferencesResponse(
            request.UserId,
            request.GymId,
            preferenceDtos,
            preference.QuietHoursStart,
            preference.QuietHoursEnd);

        return Result.Success(response);
    }
}

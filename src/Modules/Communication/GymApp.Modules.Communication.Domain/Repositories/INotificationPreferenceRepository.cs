using GymApp.Modules.Communication.Domain.Entities;

namespace GymApp.Modules.Communication.Domain.Repositories;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByUserAsync(Guid userId, Guid? gymId = null, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
    void Update(NotificationPreference preference);
}

using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Domain.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetByUserAsync(Guid userId, Guid? gymId, PageRequest page, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, Guid? gymId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    void Update(Notification notification);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

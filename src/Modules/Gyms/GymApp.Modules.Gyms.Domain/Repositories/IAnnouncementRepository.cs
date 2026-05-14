using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Announcement>> GetByGymIdAsync(Guid gymId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default);
}

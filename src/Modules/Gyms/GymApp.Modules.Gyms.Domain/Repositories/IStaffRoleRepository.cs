using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IStaffRoleRepository
{
    Task<List<StaffRole>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<StaffRole?> GetByUserAndGymAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(StaffRole staffRole, CancellationToken cancellationToken = default);
    void Update(StaffRole staffRole);
    void Remove(StaffRole staffRole);
}

using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface ICheckInRepository
{
    Task<List<CheckIn>> GetByGymIdAsync(Guid gymId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<CheckIn?> GetLatestForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task AddAsync(CheckIn checkIn, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid gymId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

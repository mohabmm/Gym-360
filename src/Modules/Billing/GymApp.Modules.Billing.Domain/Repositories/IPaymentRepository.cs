using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByGymIdAsync(Guid gymId, DateTime from, DateTime to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    void Update(Payment payment);
    Task<decimal> GetRevenueAsync(Guid gymId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

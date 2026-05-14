using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IMemberNoteRepository
{
    Task<List<MemberNote>> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task AddAsync(MemberNote note, CancellationToken cancellationToken = default);
}

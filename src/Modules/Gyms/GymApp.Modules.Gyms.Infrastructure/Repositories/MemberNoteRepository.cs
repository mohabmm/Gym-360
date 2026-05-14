using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class MemberNoteRepository : IMemberNoteRepository
{
    private readonly GymsDbContext _dbContext;

    public MemberNoteRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MemberNote>> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MemberNotes
            .AsNoTracking()
            .Where(n => n.GymId == gymId && n.MemberId == memberId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MemberNote note, CancellationToken cancellationToken = default)
    {
        await _dbContext.MemberNotes.AddAsync(note, cancellationToken);
    }
}

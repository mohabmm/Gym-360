using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class OtpCodeRepository : IOtpCodeRepository
{
    private readonly IdentityDbContext _dbContext;

    public OtpCodeRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OtpCode?> GetLatestAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OtpCodes
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetCountInWindowAsync(Guid userId, OtpPurpose purpose, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var windowStart = DateTime.UtcNow.Subtract(timeWindow);

        return await _dbContext.OtpCodes
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId && o.Purpose == purpose && o.CreatedAt >= windowStart, cancellationToken);
    }

    public async Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.OtpCodes.AddAsync(otpCode, cancellationToken);
    }
}

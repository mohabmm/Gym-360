using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class GymMemberDirectory : IGymMemberDirectory
{
    private readonly GymsDbContext _dbContext;

    public GymMemberDirectory(GymsDbContext dbContext) => _dbContext = dbContext;

    public async Task<Guid?> FindMemberIdByNumberAsync(Guid gymId, string memberNumber, CancellationToken cancellationToken)
    {
        var trimmed = memberNumber.Trim();
        var withPrefix = trimmed.StartsWith("M-", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : "M-" + trimmed;
        var row = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId && (m.MemberNumber == trimmed || m.MemberNumber == withPrefix))
            .Select(m => new { m.MemberId })
            .FirstOrDefaultAsync(cancellationToken);

        return row?.MemberId;
    }

    public async Task<string> RegisterAsync(GymMemberRegistration registration, CancellationToken cancellationToken)
    {
        var (gymId, memberId, preferredNumber, section) = registration;
        var existing = await _dbContext.GymMembers
            .FirstOrDefaultAsync(m => m.GymId == gymId && m.MemberId == memberId, cancellationToken);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(section) && existing.Section != section)
                existing.SetSection(section);
            return existing.MemberNumber;
        }

        var number = !string.IsNullOrWhiteSpace(preferredNumber)
            ? preferredNumber.Trim()
            : await GenerateNextNumberAsync(gymId, cancellationToken);

        var row = GymMember.Create(gymId, memberId, number, section);
        _dbContext.GymMembers.Add(row);
        return number;
    }

    public async Task<string?> GetMemberNumberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken)
    {
        return await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId && m.MemberId == memberId)
            .Select(m => m.MemberNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string> GenerateNextNumberAsync(Guid gymId, CancellationToken ct)
    {
        // Take the highest numeric MemberNumber for the gym and increment.
        // Non-numeric numbers (manually assigned) are ignored by this generator.
        var numbers = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId)
            .Select(m => m.MemberNumber)
            .ToListAsync(ct);

        var highest = 0;
        foreach (var n in numbers)
            if (int.TryParse(n, out var v) && v > highest) highest = v;

        return (highest + 1).ToString("D4");
    }
}

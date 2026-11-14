using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

/// <summary>
/// Deletes the user's gym-side records. Kept: classes they taught (gym schedule) and
/// data-import audit rows; check-ins they processed as staff lose the staff reference.
/// </summary>
internal sealed class GymsUserDataEraser : IUserDataEraser
{
    private readonly GymsDbContext _db;

    public GymsUserDataEraser(GymsDbContext db) => _db = db;

    public string ModuleName => "Gyms";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await _db.CheckIns.Where(c => c.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.CheckIns.Where(c => c.StaffId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.StaffId, (Guid?)null), cancellationToken);
        count += await _db.ClassBookings.Where(b => b.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.GymMembers.Where(m => m.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.MemberNotes.Where(n => n.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.MemberOnboardings.Where(o => o.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.StaffRoles.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.TrainerRates.Where(r => r.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.GymTrainerRequests.Where(r => r.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        return count;
    }
}

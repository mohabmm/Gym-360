using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Communication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Communication.Infrastructure.Services;

/// <summary>
/// Deletes the user's notifications, preferences, device tokens, and every conversation
/// they take part in (all messages cascade) — a chat with a deleted user has no
/// remaining counterpart.
/// </summary>
internal sealed class CommunicationUserDataEraser : IUserDataEraser
{
    private readonly CommunicationDbContext _db;

    public CommunicationUserDataEraser(CommunicationDbContext db) => _db = db;

    public string ModuleName => "Communication";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await _db.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.NotificationPreferences.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.DeviceTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.Messages.Where(m => m.SenderId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await DeleteConversationsAsync(userId, cancellationToken);
        return count;
    }

    // ParticipantIds is a JSON column behind a value converter, so membership can't be
    // filtered in SQL (same constraint as ConversationRepository.GetByParticipantsAsync).
    private async Task<int> DeleteConversationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var conversations = await _db.Conversations
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParticipantIds })
            .ToListAsync(cancellationToken);

        var ids = conversations.Where(c => c.ParticipantIds.Contains(userId)).Select(c => c.Id).ToList();
        if (ids.Count == 0)
            return 0;

        return await _db.Conversations.Where(c => ids.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
    }
}

using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Communication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Communication.Infrastructure.Repositories;

internal sealed class ConversationRepository : IConversationRepository
{
    private readonly CommunicationDbContext _dbContext;

    public ConversationRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByParticipantsAsync(
        Guid gymId,
        Guid participant1,
        Guid participant2,
        CancellationToken cancellationToken = default)
    {
        // ParticipantIds is stored as JSON (nvarchar) via a value converter, so .Contains()
        // cannot be translated to SQL. Pull gym conversations and filter in-memory.
        // Tracked (not AsNoTracking): callers may mutate the returned conversation (e.g. append
        // a Message), and EF's change tracker detects the added child entity automatically.
        var gymConversations = await _dbContext.Conversations
            .Where(c => c.GymId == gymId)
            .ToListAsync(cancellationToken);

        return gymConversations.FirstOrDefault(c =>
            c.ParticipantIds.Contains(participant1) && c.ParticipantIds.Contains(participant2));
    }

    public async Task<(List<Conversation> Items, int TotalCount)> GetByUserAsync(
        Guid gymId,
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        // ParticipantIds is stored as JSON (nvarchar) via a value converter, so .Contains()
        // cannot be translated to SQL. Pull gym conversations and filter in-memory.
        var gymConversations = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.GymId == gymId)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .ToListAsync(cancellationToken);

        var userConversations = gymConversations
            .Where(c => c.ParticipantIds.Contains(userId))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToList();

        var totalCount = userConversations.Count;
        var items = userConversations
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUnreadCountsByConversationAsync(
        IEnumerable<Guid> conversationIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ids = conversationIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, int>();

        var counts = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => ids.Contains(m.ConversationId)
                && m.SenderId != userId
                && !m.IsRead)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.ConversationId, x => x.Count);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
    }

    public void Update(Conversation conversation)
    {
        _dbContext.Conversations.Update(conversation);
    }
}

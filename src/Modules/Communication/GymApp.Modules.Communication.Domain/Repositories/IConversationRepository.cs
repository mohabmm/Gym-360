using GymApp.Modules.Communication.Domain.Entities;

namespace GymApp.Modules.Communication.Domain.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByParticipantsAsync(Guid gymId, Guid participant1, Guid participant2, CancellationToken cancellationToken = default);
    Task<(List<Conversation> Items, int TotalCount)> GetByUserAsync(Guid gymId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetUnreadCountsByConversationAsync(IEnumerable<Guid> conversationIds, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    void Update(Conversation conversation);
}

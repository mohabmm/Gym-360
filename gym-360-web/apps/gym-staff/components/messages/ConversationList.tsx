import { formatDate } from '@gym360/ui';
import { MessageSquare, AlertCircle } from 'lucide-react';
import type { ChatConversation, ChatParticipant } from '@/lib/hooks/useChat';

interface ItemProps {
  conversation: ChatConversation;
  other: ChatParticipant | undefined;
  active: boolean;
  onSelect: (id: string) => void;
}

function ConversationItem({ conversation: c, other, active, onSelect }: ItemProps) {
  return (
    <button
      onClick={() => onSelect(c.id)}
      className={`w-full text-left px-3 py-3 border-b hover:bg-muted/50 transition-colors ${
        active ? 'bg-muted' : ''
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="text-sm font-medium truncate">{other?.name ?? 'Unknown'}</p>
          <p className="text-xs text-muted-foreground truncate">
            {c.lastMessage?.content ?? '(no messages yet)'}
          </p>
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          {c.lastMessageAt && (
            <span className="text-[10px] text-muted-foreground">
              {formatDate(c.lastMessageAt)}
            </span>
          )}
          {c.unreadCount > 0 && (
            <span className="text-[10px] font-bold rounded-full bg-primary text-primary-foreground px-2 py-0.5">
              {c.unreadCount}
            </span>
          )}
        </div>
      </div>
    </button>
  );
}

interface Props {
  conversations: ChatConversation[];
  loading: boolean;
  error: boolean;
  selectedId: string | null;
  onSelect: (id: string) => void;
  otherParticipant: (c: ChatConversation) => ChatParticipant | undefined;
}

export function ConversationList({ conversations, loading, error, selectedId, onSelect, otherParticipant }: Props) {
  return (
    <div className="md:col-span-1 border rounded-xl bg-card overflow-hidden flex flex-col">
      <div className="p-3 border-b flex items-center gap-2 text-sm font-medium">
        <MessageSquare className="h-4 w-4" />
        <span>Conversations ({conversations.length})</span>
      </div>
      <div className="flex-1 overflow-y-auto">
        {loading && <p className="p-4 text-sm text-muted-foreground">Loading...</p>}
        {error && (
          <div className="p-4 flex items-center gap-2 text-sm text-red-600">
            <AlertCircle className="h-4 w-4" />
            Failed to load conversations.
          </div>
        )}
        {!loading && conversations.length === 0 && (
          <p className="p-4 text-sm text-muted-foreground">No conversations yet.</p>
        )}
        {conversations.map((c) => (
          <ConversationItem
            key={c.id}
            conversation={c}
            other={otherParticipant(c)}
            active={c.id === selectedId}
            onSelect={onSelect}
          />
        ))}
      </div>
    </div>
  );
}

import type { RefObject } from 'react';
import { formatDate } from '@gym360/ui';
import { Send } from 'lucide-react';
import type { ChatMessage, ChatParticipant } from '@/lib/hooks/useChat';

function MessageBubble({ message: m, mine }: { message: ChatMessage; mine: boolean }) {
  return (
    <div className={`flex ${mine ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[70%] rounded-2xl px-3 py-2 text-sm ${
          mine
            ? 'bg-primary text-primary-foreground rounded-br-sm'
            : 'bg-muted rounded-bl-sm'
        }`}
      >
        {!mine && (
          <p className="text-[10px] font-medium opacity-70 mb-0.5">{m.senderName}</p>
        )}
        <p className="whitespace-pre-wrap break-words">{m.content}</p>
        <p className="text-[10px] opacity-60 mt-1">{formatDate(m.sentAt)}</p>
      </div>
    </div>
  );
}

interface Props {
  other: ChatParticipant | undefined;
  messages: ChatMessage[];
  myUserId: string | undefined;
  scrollRef: RefObject<HTMLDivElement>;
  draft: string;
  onDraftChange: (draft: string) => void;
  onSend: (e: React.FormEvent) => void;
  sending: boolean;
}

export function MessageThread({ other, messages, myUserId, scrollRef, draft, onDraftChange, onSend, sending }: Props) {
  return (
    <>
      <div className="p-3 border-b">
        <p className="text-sm font-semibold">{other?.name}</p>
        <p className="text-xs text-muted-foreground">
          {other?.role ?? '—'}
        </p>
      </div>
      <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-3">
        {messages.map((m) => (
          <MessageBubble key={m.id} message={m} mine={m.senderId === myUserId} />
        ))}
        {messages.length === 0 && (
          <p className="text-center text-sm text-muted-foreground py-8">
            No messages yet — say hello.
          </p>
        )}
      </div>
      <form onSubmit={onSend} className="p-3 border-t flex items-center gap-2">
        <input
          value={draft}
          onChange={(e) => onDraftChange(e.target.value)}
          placeholder="Type a message..."
          className="flex-1 h-10 rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
        />
        <button
          type="submit"
          disabled={!draft.trim() || sending}
          className="inline-flex items-center gap-1 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
        >
          <Send className="h-4 w-4" />
          {sending ? 'Sending...' : 'Send'}
        </button>
      </form>
    </>
  );
}

'use client';

import { useEffect, useMemo, useState, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { PageHeader, formatDate } from '@gymapp/ui';
import { Send, MessageSquare, AlertCircle } from 'lucide-react';
import {
  useConversations,
  useMessages,
  useSendMessage,
  useMarkConversationRead,
  type ChatConversation,
} from '@/lib/hooks/useChat';
import { useChatHub } from '@/lib/hooks/useChatHub';

export default function MessagesPage() {
  const { data: session } = useSession();
  const myUserId = (session?.user as any)?.id as string | undefined;

  const { data: conversations = [], isLoading: convLoading, isError: convError } = useConversations();
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Default-select first conversation
  useEffect(() => {
    if (!selectedId && conversations.length > 0) setSelectedId(conversations[0].id);
  }, [conversations, selectedId]);

  const selected = useMemo<ChatConversation | undefined>(
    () => conversations.find((c) => c.id === selectedId),
    [conversations, selectedId],
  );

  // Real-time hub: server pushes message_received / messages_read into React Query cache.
  useChatHub(selectedId);

  const { data: messages = [] } = useMessages(selectedId);
  const sendMutation = useSendMessage(selectedId);
  const markRead = useMarkConversationRead();

  const [draft, setDraft] = useState('');
  const scrollRef = useRef<HTMLDivElement>(null);

  // Mark conversation read when opened
  useEffect(() => {
    if (selectedId) markRead.mutate(selectedId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedId]);

  // Auto-scroll to latest message
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages.length, selectedId]);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!draft.trim() || !selectedId) return;
    const content = draft.trim();
    setDraft('');
    try {
      await sendMutation.mutateAsync(content);
    } catch {
      setDraft(content); // restore on failure
    }
  };

  const otherParticipant = (c: ChatConversation) =>
    c.participants.find((p) => p.userId !== myUserId) ?? c.participants[0];

  return (
    <div className="space-y-4">
      <PageHeader
        title="Messages"
        description="Real-time conversations with members and trainers"
      />

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 h-[70vh]">
        {/* Conversations list */}
        <div className="md:col-span-1 border rounded-xl bg-card overflow-hidden flex flex-col">
          <div className="p-3 border-b flex items-center gap-2 text-sm font-medium">
            <MessageSquare className="h-4 w-4" />
            <span>Conversations ({conversations.length})</span>
          </div>
          <div className="flex-1 overflow-y-auto">
            {convLoading && <p className="p-4 text-sm text-muted-foreground">Loading...</p>}
            {convError && (
              <div className="p-4 flex items-center gap-2 text-sm text-red-600">
                <AlertCircle className="h-4 w-4" />
                Failed to load conversations.
              </div>
            )}
            {!convLoading && conversations.length === 0 && (
              <p className="p-4 text-sm text-muted-foreground">No conversations yet.</p>
            )}
            {conversations.map((c) => {
              const other = otherParticipant(c);
              const active = c.id === selectedId;
              return (
                <button
                  key={c.id}
                  onClick={() => setSelectedId(c.id)}
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
            })}
          </div>
        </div>

        {/* Thread */}
        <div className="md:col-span-2 border rounded-xl bg-card overflow-hidden flex flex-col">
          {!selected ? (
            <div className="flex-1 flex items-center justify-center text-sm text-muted-foreground">
              Select a conversation
            </div>
          ) : (
            <>
              <div className="p-3 border-b">
                <p className="text-sm font-semibold">{otherParticipant(selected)?.name}</p>
                <p className="text-xs text-muted-foreground">
                  {otherParticipant(selected)?.role ?? '—'}
                </p>
              </div>
              <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-3">
                {messages.map((m) => {
                  const mine = m.senderId === myUserId;
                  return (
                    <div
                      key={m.id}
                      className={`flex ${mine ? 'justify-end' : 'justify-start'}`}
                    >
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
                })}
                {messages.length === 0 && (
                  <p className="text-center text-sm text-muted-foreground py-8">
                    No messages yet — say hello.
                  </p>
                )}
              </div>
              <form onSubmit={handleSend} className="p-3 border-t flex items-center gap-2">
                <input
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  placeholder="Type a message..."
                  className="flex-1 h-10 rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
                />
                <button
                  type="submit"
                  disabled={!draft.trim() || sendMutation.isPending}
                  className="inline-flex items-center gap-1 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                  <Send className="h-4 w-4" />
                  {sendMutation.isPending ? 'Sending...' : 'Send'}
                </button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

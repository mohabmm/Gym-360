'use client';

import { useEffect, useMemo, useState, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { PageHeader } from '@gym360/ui';
import {
  useConversations,
  useMessages,
  useSendMessage,
  useMarkConversationRead,
  type ChatConversation,
} from '@/lib/hooks/useChat';
import { useChatHub } from '@/lib/hooks/useChatHub';
import { ConversationList } from '@/components/messages/ConversationList';
import { MessageThread } from '@/components/messages/MessageThread';

function useSelectedConversation(conversations: ChatConversation[]) {
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Default-select first conversation
  useEffect(() => {
    if (!selectedId && conversations.length > 0) setSelectedId(conversations[0].id);
  }, [conversations, selectedId]);

  const selected = useMemo<ChatConversation | undefined>(
    () => conversations.find((c) => c.id === selectedId),
    [conversations, selectedId],
  );
  return { selectedId, setSelectedId, selected };
}

function scrollToBottom(el: HTMLDivElement | null) {
  if (el) el.scrollTop = el.scrollHeight;
}

export default function MessagesPage() {
  const { data: session } = useSession();
  const myUserId = (session?.user as any)?.id as string | undefined;

  const { data: conversations = [], isLoading: convLoading, isError: convError } = useConversations();
  const { selectedId, setSelectedId, selected } = useSelectedConversation(conversations);

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
  useEffect(() => scrollToBottom(scrollRef.current), [messages.length, selectedId]);

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
        <ConversationList
          conversations={conversations}
          loading={convLoading}
          error={convError}
          selectedId={selectedId}
          onSelect={setSelectedId}
          otherParticipant={otherParticipant}
        />

        {/* Thread */}
        <div className="md:col-span-2 border rounded-xl bg-card overflow-hidden flex flex-col">
          {!selected ? (
            <div className="flex-1 flex items-center justify-center text-sm text-muted-foreground">
              Select a conversation
            </div>
          ) : (
            <MessageThread
              other={otherParticipant(selected)}
              messages={messages}
              myUserId={myUserId}
              scrollRef={scrollRef}
              draft={draft}
              onDraftChange={setDraft}
              onSend={handleSend}
              sending={sendMutation.isPending}
            />
          )}
        </div>
      </div>
    </div>
  );
}

'use client';

import { useEffect, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { useQueryClient } from '@tanstack/react-query';
import { useSignalR } from '@gymapp/ui';

interface MessagePayload {
  conversationId: string;
  id: string;
  senderId: string;
  content: string;
  mediaUrl: string | null;
  mediaType: string | null;
  sentAt: string;
}

interface MessagesReadPayload {
  conversationId: string;
  readerId: string;
  readAt: string;
}

interface ConversationCreatedPayload {
  conversationId: string;
  initiatorId: string;
  otherParticipantId: string;
  gymId: string;
}

const HUB_URL = `${(process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1').replace(/\/api\/v\d+$/, '')}/hubs/chat`;

/**
 * Real-time chat client. Subscribes to /hubs/chat once per session, then JoinConversation
 * is invoked whenever the user opens a thread (caller passes the focused conversationId).
 *
 * On `message_received` / `messages_read` / `conversation_created` events the relevant
 * React Query caches are invalidated so views update without a manual refresh.
 */
export function useChatHub(focusedConversationId?: string | null) {
  const { data: session } = useSession();
  const accessToken = session?.accessToken as string | undefined;
  const qc = useQueryClient();
  const joinedRef = useRef<string | null>(null);

  const { connection, connectionState, on, off, invoke } = useSignalR({
    hubUrl: HUB_URL,
    accessToken,
    autoReconnect: true,
  });

  // Wire event handlers once per connection (re-runs when connection swaps).
  useEffect(() => {
    if (!connection) return;

    const onMessage = (...args: unknown[]) => {
      const msg = args[0] as MessagePayload | undefined;
      if (!msg) return;
      qc.invalidateQueries({ queryKey: ['conversation-messages', msg.conversationId] });
      qc.invalidateQueries({ queryKey: ['conversations'] });
    };

    const onRead = (...args: unknown[]) => {
      const r = args[0] as MessagesReadPayload | undefined;
      if (!r) return;
      qc.invalidateQueries({ queryKey: ['conversation-messages', r.conversationId] });
      qc.invalidateQueries({ queryKey: ['conversations'] });
    };

    const onConvCreated = (...args: unknown[]) => {
      const c = args[0] as ConversationCreatedPayload | undefined;
      if (!c) return;
      qc.invalidateQueries({ queryKey: ['conversations'] });
    };

    on('message_received', onMessage);
    on('messages_read', onRead);
    on('conversation_created', onConvCreated);

    return () => {
      off('message_received', onMessage);
      off('messages_read', onRead);
      off('conversation_created', onConvCreated);
    };
  }, [connection, qc, on, off]);

  // Join/leave conversation groups as the focused thread changes.
  useEffect(() => {
    if (!connection || connectionState !== 'Connected') return;
    const next = focusedConversationId ?? null;
    const prev = joinedRef.current;
    if (prev === next) return;

    (async () => {
      try {
        if (prev) await invoke('LeaveConversation', prev);
        if (next) await invoke('JoinConversation', next);
        joinedRef.current = next;
      } catch {
        // best-effort; reconnect logic will re-invoke when state re-stabilizes
      }
    })();
  }, [connection, connectionState, focusedConversationId, invoke]);

  return { connectionState };
}

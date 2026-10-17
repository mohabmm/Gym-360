'use client';

import { useEffect, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { useQueryClient, type QueryClient } from '@tanstack/react-query';
import { useSignalR } from '@gym360/ui';

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

type HubHandler = (...args: unknown[]) => void;
type HubSubscribe = (event: string, handler: HubHandler) => void;

// Build the event -> handler map; each handler invalidates the caches the event affects.
function chatEventHandlers(qc: QueryClient): Record<string, HubHandler> {
  const onMessage: HubHandler = (...args) => {
    const msg = args[0] as MessagePayload | undefined;
    if (!msg) return;
    qc.invalidateQueries({ queryKey: ['conversation-messages', msg.conversationId] });
    qc.invalidateQueries({ queryKey: ['conversations'] });
  };

  const onRead: HubHandler = (...args) => {
    const r = args[0] as MessagesReadPayload | undefined;
    if (!r) return;
    qc.invalidateQueries({ queryKey: ['conversation-messages', r.conversationId] });
    qc.invalidateQueries({ queryKey: ['conversations'] });
  };

  const onConvCreated: HubHandler = (...args) => {
    const c = args[0] as ConversationCreatedPayload | undefined;
    if (!c) return;
    qc.invalidateQueries({ queryKey: ['conversations'] });
  };

  return {
    message_received: onMessage,
    messages_read: onRead,
    conversation_created: onConvCreated,
  };
}

function subscribeChatEvents(qc: QueryClient, on: HubSubscribe, off: HubSubscribe) {
  const handlers = Object.entries(chatEventHandlers(qc));
  handlers.forEach(([event, handler]) => on(event, handler));
  return () => handlers.forEach(([event, handler]) => off(event, handler));
}

type HubInvoke = (method: string, ...args: unknown[]) => Promise<unknown>;

async function switchConversation(invoke: HubInvoke, prev: string | null, next: string | null) {
  if (prev) await invoke('LeaveConversation', prev);
  if (next) await invoke('JoinConversation', next);
}

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
    return subscribeChatEvents(qc, on, off);
  }, [connection, qc, on, off]);

  // Join/leave conversation groups as the focused thread changes.
  useEffect(() => {
    if (!connection || connectionState !== 'Connected') return;
    const next = focusedConversationId ?? null;
    const prev = joinedRef.current;
    if (prev === next) return;

    (async () => {
      try {
        await switchConversation(invoke, prev, next);
        joinedRef.current = next;
      } catch {
        // best-effort; reconnect logic will re-invoke when state re-stabilizes
      }
    })();
  }, [connection, connectionState, focusedConversationId, invoke]);

  return { connectionState };
}

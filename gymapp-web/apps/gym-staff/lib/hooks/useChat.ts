import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';

export interface ChatParticipant {
  userId: string;
  name: string;
  photoUrl: string | null;
  role: string;
}

export interface ChatLastMessage {
  id: string;
  senderId: string;
  content: string;
  sentAt: string;
}

export interface ChatConversation {
  id: string;
  gymId: string;
  type: string;
  participants: ChatParticipant[];
  unreadCount: number;
  createdAt: string;
  updatedAt: string;
  lastMessageAt: string | null;
  lastMessage: ChatLastMessage | null;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  senderId: string;
  senderName: string;
  senderPhotoUrl: string | null;
  content: string;
  mediaUrl: string | null;
  mediaType: string;
  isRead: boolean;
  readAt: string | null;
  sentAt: string;
}

export function useConversations() {
  return useQuery<ChatConversation[]>({
    queryKey: ['conversations'],
    queryFn: async () => {
      const res = await apiClient.get('/communication/conversations', {
        params: { page: 1, pageSize: 50 },
      });
      return res.data?.data?.items ?? [];
    },
  });
}

export function useMessages(conversationId: string | null) {
  return useQuery<ChatMessage[]>({
    queryKey: ['conversation-messages', conversationId],
    queryFn: async () => {
      if (!conversationId) return [];
      const res = await apiClient.get(`/communication/conversations/${conversationId}/messages`, {
        params: { page: 1, pageSize: 100 },
      });
      return res.data?.data?.items ?? [];
    },
    enabled: !!conversationId,
  });
}

export function useSendMessage(conversationId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (content: string) => {
      if (!conversationId) throw new Error('No conversation selected');
      const res = await apiClient.post(`/communication/conversations/${conversationId}/messages`, { content });
      return res.data;
    },
    onSuccess: () => {
      if (conversationId) {
        qc.invalidateQueries({ queryKey: ['conversation-messages', conversationId] });
        qc.invalidateQueries({ queryKey: ['conversations'] });
      }
    },
  });
}

export function useMarkConversationRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (conversationId: string) => {
      const res = await apiClient.put(`/communication/conversations/${conversationId}/read`);
      return res.data;
    },
    onSuccess: (_data, conversationId) => {
      qc.invalidateQueries({ queryKey: ['conversation-messages', conversationId] });
      qc.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

export function useStartConversation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ gymId, participant2 }: { gymId: string; participant2: string }) => {
      const res = await apiClient.post('/communication/conversations', { gymId, participant2 });
      return res.data?.data as string;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

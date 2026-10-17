import { useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import type { AxiosResponse } from 'axios';
import apiClient from '@/lib/api-client';

/** Session-derived scope passed to every gym mutation. */
export interface GymScope {
  gymId: string | undefined;
  userId: string | undefined;
}

export function useGymScope(): GymScope {
  const { data: session } = useSession();
  return { gymId: session?.user?.gymId, userId: session?.user?.id };
}

export function useGymId() {
  return useGymScope().gymId;
}

/** Unwraps the `{ data: { items } }` / `{ data: [] }` envelope shared by list endpoints. */
export function listData<T>(res: AxiosResponse): T[] {
  return res.data.data?.items ?? res.data.data ?? [];
}

export async function postData(url: string, body?: unknown) {
  const res = await apiClient.post(url, body);
  return res.data;
}

export async function putData(url: string, body?: unknown) {
  const res = await apiClient.put(url, body);
  return res.data;
}

/**
 * useMutation bound to the session's gym scope that invalidates one query key on success
 * (the returned promise is awaited by react-query, same as `onSuccess: () => qc.invalidateQueries(...)`).
 */
export function useGymMutation<TVars, TData = unknown>(
  mutationFn: (vars: TVars, scope: GymScope) => Promise<TData>,
  invalidateKey: (scope: GymScope) => QueryKey,
) {
  const scope = useGymScope();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (vars: TVars) => mutationFn(vars, scope),
    onSuccess: () => qc.invalidateQueries({ queryKey: invalidateKey(scope) }),
  });
}

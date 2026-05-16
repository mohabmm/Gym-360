'use client';

import { useQuery, useMutation, useQueryClient, type UseQueryOptions, type UseMutationOptions } from '@tanstack/react-query';
import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '../lib/api-client';
import type { ApiResponse } from '../types/api-response';

export function useApiQuery<T>(
  queryKey: string[],
  url: string,
  params?: Record<string, unknown>,
  options?: Omit<UseQueryOptions<ApiResponse<T>, Error>, 'queryKey' | 'queryFn'>
) {
  return useQuery<ApiResponse<T>, Error>({
    queryKey,
    queryFn: () => apiGet<T>(url, params),
    ...options,
  });
}

export function useApiMutation<TData, TVariables = unknown>(
  url: string,
  method: 'post' | 'put' | 'patch' | 'delete' = 'post',
  options?: Omit<UseMutationOptions<ApiResponse<TData>, Error, TVariables>, 'mutationFn'>
) {
  const queryClient = useQueryClient();
  const methods = { post: apiPost, put: apiPut, patch: apiPatch, delete: apiDelete };

  return useMutation<ApiResponse<TData>, Error, TVariables>({
    mutationFn: async (variables: TVariables) => {
      const fn = methods[method];
      if (method === 'delete') {
        return fn<TData>(url);
      }
      return (fn as typeof apiPost)<TData>(url, variables);
    },
    onSuccess: () => {
      // Allow callers to specify which queries to invalidate
    },
    ...options,
  });
}

export function useInvalidateQueries() {
  const queryClient = useQueryClient();

  return {
    invalidate: (queryKey: string[]) => {
      void queryClient.invalidateQueries({ queryKey });
    },
    invalidateAll: () => {
      void queryClient.invalidateQueries();
    },
    invalidateMatching: (predicate: string) => {
      void queryClient.invalidateQueries({
        predicate: (query) =>
          query.queryKey.some((key) => typeof key === 'string' && key.includes(predicate)),
      });
    },
  };
}

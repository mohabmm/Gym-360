import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse, ApiError } from '../types/api-response';

let getAccessToken: (() => string | null) | undefined;
let getActiveGymId: (() => string | null) | undefined;
let onUnauthorized: (() => void) | undefined;

export function configureApiClient(config: {
  getAccessToken: () => string | null;
  getActiveGymId?: () => string | null;
  onUnauthorized?: () => void;
}): void {
  getAccessToken = config.getAccessToken;
  getActiveGymId = config.getActiveGymId;
  onUnauthorized = config.onUnauthorized;
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'Accept-Language': 'ar',
  },
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken?.();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  const gymId = getActiveGymId?.();
  if (gymId) {
    config.headers['X-Gym-Id'] = gymId;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      onUnauthorized?.();
    }
    return Promise.reject(error);
  }
);

export async function apiGet<T>(url: string, params?: Record<string, unknown>): Promise<ApiResponse<T>> {
  const response = await apiClient.get<ApiResponse<T>>(url, { params });
  return response.data;
}

export async function apiPost<T>(url: string, data?: unknown): Promise<ApiResponse<T>> {
  const response = await apiClient.post<ApiResponse<T>>(url, data);
  return response.data;
}

export async function apiPut<T>(url: string, data?: unknown): Promise<ApiResponse<T>> {
  const response = await apiClient.put<ApiResponse<T>>(url, data);
  return response.data;
}

export async function apiPatch<T>(url: string, data?: unknown): Promise<ApiResponse<T>> {
  const response = await apiClient.patch<ApiResponse<T>>(url, data);
  return response.data;
}

export async function apiDelete<T>(url: string): Promise<ApiResponse<T>> {
  const response = await apiClient.delete<ApiResponse<T>>(url);
  return response.data;
}

/**
 * Extracts the typed ApiError[] from a failed axios request. Any axios/network failure is
 * collapsed into a single synthetic error so callers can always rely on the same shape.
 */
export function extractApiErrors(error: unknown): ApiError[] {
  if (axios.isAxiosError(error)) {
    const body = (error as AxiosError<ApiResponse<unknown>>).response?.data;
    if (body && Array.isArray(body.errors) && body.errors.length > 0) {
      return body.errors as ApiError[];
    }
    return [{ code: 'NETWORK_ERROR', message: error.message || 'Network request failed.' }];
  }
  return [{ code: 'UNKNOWN', message: 'An unexpected error occurred.' }];
}

export function firstErrorCode(error: unknown): string | undefined {
  return extractApiErrors(error)[0]?.code;
}

export function isFeatureGatedError(error: unknown): boolean {
  return firstErrorCode(error) === 'PLAN.FEATURE_NOT_AVAILABLE';
}

export function isGymSuspendedError(error: unknown): boolean {
  return firstErrorCode(error) === 'GYM_SUSPENDED';
}

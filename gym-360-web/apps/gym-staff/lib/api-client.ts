import axios from 'axios';
import { getSession } from 'next-auth/react';

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1';

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use(async (config) => {
  // In client context, get session from next-auth
  if (typeof window !== 'undefined') {
    const session = await getSession();
    if (session?.accessToken) {
      config.headers.Authorization = `Bearer ${session.accessToken}`;
    }
    // Gym ID header for multi-gym scoping
    const gymId = session?.user?.gymId;
    if (gymId) {
      config.headers['X-Gym-Id'] = gymId;
    }
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Surface the backend's real error message (errors[0].message) on error.message
    // so toasts show e.g. "An account with this phone already exists..." instead of
    // axios's generic "Request failed with status code 400".
    const apiMsg = error?.response?.data?.errors?.[0]?.message;
    if (typeof apiMsg === 'string' && apiMsg) {
      error.message = apiMsg;
    }
    if (error.response?.status === 401 && typeof window !== 'undefined') {
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;

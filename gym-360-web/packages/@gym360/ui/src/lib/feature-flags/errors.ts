export const FEATURE_DISABLED_PREFIX = 'FEATURE_DISABLED:';
/** Window event fired when the API answers FEATURE_DISABLED; the flags hook refetches on it. */
export const FEATURE_DISABLED_EVENT = 'gym360:feature-disabled';

/** The flag key from a `403 FEATURE_DISABLED:<key>` error, or null for any other error. */
export function featureDisabledKey(error: unknown): string | null {
  const code = (error as { response?: { data?: { errors?: { code?: unknown }[] } } } | null)?.response?.data?.errors?.[0]
    ?.code;
  if (typeof code !== 'string') return null;
  return code.startsWith(FEATURE_DISABLED_PREFIX) ? code.slice(FEATURE_DISABLED_PREFIX.length) : null;
}

/**
 * Call from an axios response interceptor. When the error is FEATURE_DISABLED it tells the
 * flags hook to refresh and swaps the raw server text for a friendly message.
 */
export function handleFeatureDisabledError(error: unknown): void {
  const key = featureDisabledKey(error);
  if (!key) return;
  if (error instanceof Error) {
    error.message = 'هذه الميزة غير متاحة لصالتك حاليًا · This feature is not available for your gym.';
  }
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent(FEATURE_DISABLED_EVENT, { detail: { key } }));
  }
}

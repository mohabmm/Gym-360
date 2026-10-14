type ApiErrorShape = {
  response?: { status?: number; data?: { errors?: { message?: string }[] } };
};

/** First backend error message on a failed API call, or `fallback`. */
export function apiErrorMessage(err: unknown, fallback: string): string {
  return (err as ApiErrorShape)?.response?.data?.errors?.[0]?.message ?? fallback;
}

/** HTTP status of a failed API call, when there was a response. */
export function apiErrorStatus(err: unknown): number | undefined {
  return (err as ApiErrorShape)?.response?.status;
}

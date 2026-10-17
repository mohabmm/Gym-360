/** Reads a cookie value from `document.cookie` (client only); null when absent or on the server. */
export function readClientCookie(name: string): string | null {
  if (typeof document === 'undefined') return null;
  const raw = document.cookie
    .split(';')
    .map((c) => c.trim())
    .find((c) => c.startsWith(`${name}=`));
  return raw ? decodeURIComponent(raw.split('=')[1] ?? '') : null;
}

/** Cookie value narrowed by `guard`, or null when it's missing, invalid, or equal to `current`. */
export function readChangedCookie<T extends string>(
  name: string,
  guard: (value: string) => value is T,
  current: T,
): T | null {
  const value = readClientCookie(name);
  if (!value || !guard(value)) return null;
  return value === current ? null : value;
}

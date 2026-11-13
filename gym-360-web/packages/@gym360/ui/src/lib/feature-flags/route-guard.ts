/** A path prefix and the flag that owns it. Paths not listed are always on. */
export type RouteFlagRule = { prefix: string; flags: readonly string[] };

const matches = (path: string, prefix: string) =>
  prefix === '/' ? path === '/' : path === prefix || path.startsWith(prefix + '/');

/**
 * The flags that must be ON for `path`. With several rules matching, the longest prefix wins.
 * A rule with several flags passes when any one of them is ON (e.g. a page with several tabs).
 */
const isLonger = (rule: RouteFlagRule, best: RouteFlagRule | null) => !best || rule.prefix.length > best.prefix.length;

export function flagsForPath(path: string, rules: readonly RouteFlagRule[]): readonly string[] | null {
  let best: RouteFlagRule | null = null;
  for (const rule of rules) {
    if (!matches(path, rule.prefix)) continue;
    if (isLonger(rule, best)) best = rule;
  }
  return best?.flags ?? null;
}

export function isPathEnabled(path: string, rules: readonly RouteFlagRule[], isEnabled: (key: string) => boolean): boolean {
  const flags = flagsForPath(path, rules);
  return !flags || flags.some(isEnabled);
}

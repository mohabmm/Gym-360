'use client';

import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts';
import { cn } from '@gym360/ui';
import type { PlanDistribution } from '@gym360/ui';

/**
 * Plan Distribution donut.
 *
 * The platform analytics `planDistribution` returns EVERY subscription plan —
 * including zero-count rows and stale QA/test plans. Plotting all of them made
 * the chart flaky:
 *   - zero-count slices piled overlapping "0%" labels at the pie centre
 *   - a single 100% slice rendered as a glitchy broken "C" ring (arc seam)
 *   - a local 4-colour palette fought the BE-provided per-plan `color`
 *
 * This component is the single source of truth for both the dashboard and the
 * analytics page: filter to plans with data, honour the BE `color`, kill the
 * seam (`stroke="none"`) and the entrance flicker (`isAnimationActive=false`),
 * and read plan names off a calm side legend instead of cramped inline labels.
 */

const FALLBACK = ['#3B82F6', '#8B5CF6', '#F59E0B', '#10B981', '#EF4444', '#6B7280'];
const isHex = (c?: string) => !!c && /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.test(c);

export function PlanDistributionChart({
  data,
  height = 220,
  className,
}: {
  data?: PlanDistribution[];
  height?: number;
  className?: string;
}) {
  // Only plans that actually have a gym. Drops zero-count + stale QA/test rows.
  const slices = (data ?? []).filter((p) => p.count > 0);
  const total = slices.reduce((sum, p) => sum + p.count, 0);
  // Recompute % over the visible slices (BE % is over all plans incl. zeros).
  const pct = (count: number) => (total ? Math.round((count / total) * 100) : 0);
  const colorAt = (p: PlanDistribution, i: number) =>
    isHex(p.color) ? p.color : FALLBACK[i % FALLBACK.length];

  return (
    <div className={cn('rounded-xl border bg-card p-6 shadow-sm', className)}>
      <h3 className="text-sm font-semibold mb-4">Plan Distribution</h3>
      {slices.length === 0 ? (
        <div
          className="flex items-center justify-center text-sm text-muted-foreground"
          style={{ height }}
        >
          No plan data yet
        </div>
      ) : (
        <div className="flex items-center gap-6" style={{ minHeight: height }}>
          <div style={{ width: height, height }} className="shrink-0">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={slices}
                  cx="50%"
                  cy="50%"
                  outerRadius={Math.round(height * 0.4)}
                  innerRadius={Math.round(height * 0.24)}
                  dataKey="count"
                  nameKey="plan"
                  stroke="none"
                  isAnimationActive={false}
                  paddingAngle={slices.length > 1 ? 2 : 0}
                >
                  {slices.map((entry, i) => (
                    <Cell key={`${entry.plan}-${i}`} fill={colorAt(entry, i)} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value: number, name: string) => [
                    `${value} gym${value === 1 ? '' : 's'} (${pct(value)}%)`,
                    name,
                  ]}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <ul className="flex-1 space-y-2 text-sm">
            {slices.map((entry, i) => (
              <li key={`${entry.plan}-${i}`} className="flex items-center gap-2">
                <span
                  className="h-2.5 w-2.5 shrink-0 rounded-full"
                  style={{ backgroundColor: colorAt(entry, i) }}
                />
                <span className="flex-1 truncate font-medium" title={entry.plan}>
                  {entry.plan}
                </span>
                <span className="tabular-nums text-muted-foreground">{entry.count}</span>
                <span className="w-10 text-right font-semibold tabular-nums">{pct(entry.count)}%</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

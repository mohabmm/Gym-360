'use client';

import { type ReactNode } from 'react';
import { cn } from '../lib/utils';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';

interface StatCardProps {
  icon: ReactNode;
  label: string;
  value: string | number;
  trend?: number;
  trendLabel?: string;
  className?: string;
  iconClassName?: string;
}

type TrendDirection = 'up' | 'down' | 'neutral';

const TREND_STYLES: Record<TrendDirection, { Icon: typeof Minus; iconClass: string; textClass: string }> = {
  up: { Icon: TrendingUp, iconClass: 'h-4 w-4 text-emerald-500', textClass: 'text-emerald-500' },
  down: { Icon: TrendingDown, iconClass: 'h-4 w-4 text-red-500', textClass: 'text-red-500' },
  neutral: { Icon: Minus, iconClass: 'h-4 w-4 text-muted-foreground', textClass: 'text-muted-foreground' },
};

function trendDirectionOf(trend: number): TrendDirection {
  if (trend > 0) return 'up';
  if (trend < 0) return 'down';
  return 'neutral';
}

function TrendRow({ trend, trendLabel }: { trend: number; trendLabel?: string }) {
  const { Icon, iconClass, textClass } = TREND_STYLES[trendDirectionOf(trend)];
  return (
    <div className="mt-3 flex items-center gap-1.5 text-sm">
      <Icon className={iconClass} />
      <span className={cn('font-medium', textClass)}>
        {trend > 0 ? '+' : ''}
        {trend}%
      </span>
      {trendLabel && <span className="text-muted-foreground">{trendLabel}</span>}
    </div>
  );
}

export function StatCard({
  icon,
  label,
  value,
  trend,
  trendLabel,
  className,
  iconClassName,
}: StatCardProps) {
  return (
    <div
      className={cn(
        'rounded-xl border bg-card p-6 shadow-sm hover:shadow-md transition-shadow',
        className
      )}
    >
      <div className="flex items-start justify-between">
        <div className="space-y-2">
          <p className="text-sm font-medium text-muted-foreground">{label}</p>
          <p className="text-2xl font-bold tracking-tight">{value}</p>
        </div>
        <div
          className={cn(
            'flex h-10 w-10 items-center justify-center rounded-lg',
            iconClassName ?? 'bg-primary/10 text-primary'
          )}
        >
          {icon}
        </div>
      </div>
      {trend !== undefined && <TrendRow trend={trend} trendLabel={trendLabel} />}
    </div>
  );
}

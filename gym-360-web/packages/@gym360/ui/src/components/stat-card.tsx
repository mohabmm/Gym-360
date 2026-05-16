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

export function StatCard({
  icon,
  label,
  value,
  trend,
  trendLabel,
  className,
  iconClassName,
}: StatCardProps) {
  const trendDirection = trend === undefined ? null : trend > 0 ? 'up' : trend < 0 ? 'down' : 'neutral';

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
      {trend !== undefined && (
        <div className="mt-3 flex items-center gap-1.5 text-sm">
          {trendDirection === 'up' && <TrendingUp className="h-4 w-4 text-emerald-500" />}
          {trendDirection === 'down' && <TrendingDown className="h-4 w-4 text-red-500" />}
          {trendDirection === 'neutral' && <Minus className="h-4 w-4 text-muted-foreground" />}
          <span
            className={cn(
              'font-medium',
              trendDirection === 'up' && 'text-emerald-500',
              trendDirection === 'down' && 'text-red-500',
              trendDirection === 'neutral' && 'text-muted-foreground'
            )}
          >
            {trend > 0 ? '+' : ''}
            {trend}%
          </span>
          {trendLabel && <span className="text-muted-foreground">{trendLabel}</span>}
        </div>
      )}
    </div>
  );
}

'use client';

import { type ReactNode } from 'react';
import { cn } from '../lib/utils';
import { TrendingUp, TrendingDown } from 'lucide-react';

interface KpiCardProps {
  value: string | number;
  label: string;
  change?: number;
  changeSuffix?: string;
  icon: ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info';
  className?: string;
}

const variantStyles: Record<string, string> = {
  default: 'bg-card',
  success: 'bg-emerald-50 dark:bg-emerald-950/20 border-emerald-200 dark:border-emerald-800',
  warning: 'bg-amber-50 dark:bg-amber-950/20 border-amber-200 dark:border-amber-800',
  danger: 'bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-800',
  info: 'bg-blue-50 dark:bg-blue-950/20 border-blue-200 dark:border-blue-800',
};

const iconVariantStyles: Record<string, string> = {
  default: 'bg-primary/10 text-primary',
  success: 'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400',
  warning: 'bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400',
  danger: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
  info: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
};

export function KpiCard({
  value,
  label,
  change,
  changeSuffix = 'vs last month',
  icon,
  variant = 'default',
  className,
}: KpiCardProps) {
  return (
    <div
      className={cn(
        'rounded-xl border p-5 shadow-sm hover:shadow-md transition-all duration-200',
        variantStyles[variant],
        className
      )}
    >
      <div className="flex items-center justify-between mb-3">
        <div
          className={cn(
            'flex h-10 w-10 items-center justify-center rounded-lg',
            iconVariantStyles[variant]
          )}
        >
          {icon}
        </div>
        {change !== undefined && (
          <div
            className={cn(
              'flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full',
              change >= 0 ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400' : 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
            )}
          >
            {change >= 0 ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
            {change >= 0 ? '+' : ''}{change}%
          </div>
        )}
      </div>
      <p className="text-2xl font-bold tracking-tight">{value}</p>
      <p className="text-sm text-muted-foreground mt-1">{label}</p>
      {change !== undefined && (
        <p className="text-xs text-muted-foreground">{changeSuffix}</p>
      )}
    </div>
  );
}

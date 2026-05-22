'use client';

import { type ReactNode } from 'react';
import { cn } from '../lib/utils';
import { ResponsiveContainer } from 'recharts';

interface ChartContainerProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
  height?: number;
  action?: ReactNode;
  className?: string;
}

export function ChartContainer({
  title,
  subtitle,
  children,
  height = 300,
  action,
  className,
}: ChartContainerProps) {
  return (
    <div
      className={cn(
        'rounded-xl border bg-card p-6 shadow-sm',
        className
      )}
    >
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="text-sm font-semibold">{title}</h3>
          {subtitle && (
            <p className="mt-0.5 text-xs text-muted-foreground">{subtitle}</p>
          )}
        </div>
        {action}
      </div>
      <div style={{ height }}>
        <ResponsiveContainer width="100%" height="100%">
          {children as React.ReactElement}
        </ResponsiveContainer>
      </div>
    </div>
  );
}

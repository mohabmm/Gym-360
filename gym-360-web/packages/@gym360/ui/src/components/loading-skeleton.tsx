'use client';

import type React from 'react';
import { cn } from '../lib/utils';

interface LoadingSkeletonProps {
  className?: string;
  style?: React.CSSProperties;
}

export function LoadingSkeleton({ className, style }: LoadingSkeletonProps) {
  return (
    <div className={cn('animate-pulse rounded-lg bg-muted', className)} style={style} />
  );
}

export function CardSkeleton() {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <div className="flex items-start justify-between mb-4">
        <LoadingSkeleton className="h-4 w-24" />
        <LoadingSkeleton className="h-10 w-10 rounded-lg" />
      </div>
      <LoadingSkeleton className="h-8 w-32 mb-2" />
      <LoadingSkeleton className="h-3 w-20" />
    </div>
  );
}

export function TableSkeleton({ rows = 5, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
      {/* Header */}
      <div className="border-b bg-muted/50 px-4 py-3 flex gap-4">
        {Array.from({ length: columns }).map((_, i) => (
          <LoadingSkeleton key={`h-${i}`} className="h-4 flex-1" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }).map((_, i) => (
        <div key={`r-${i}`} className="border-b px-4 py-3 flex gap-4">
          {Array.from({ length: columns }).map((_, j) => (
            <LoadingSkeleton key={`r-${i}-${j}`} className="h-4 flex-1" />
          ))}
        </div>
      ))}
    </div>
  );
}

export function ChartSkeleton({ height = 300 }: { height?: number }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <LoadingSkeleton className="h-4 w-32 mb-1" />
      <LoadingSkeleton className="h-3 w-48 mb-4" />
      <LoadingSkeleton className="w-full" style={{ height }} />
    </div>
  );
}

export function PageSkeleton() {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <LoadingSkeleton className="h-7 w-48 mb-2" />
          <LoadingSkeleton className="h-4 w-72" />
        </div>
        <LoadingSkeleton className="h-9 w-28 rounded-lg" />
      </div>
      {/* KPI cards */}
      <div className="grid grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <CardSkeleton key={i} />
        ))}
      </div>
      {/* Chart */}
      <ChartSkeleton />
      {/* Table */}
      <TableSkeleton />
    </div>
  );
}

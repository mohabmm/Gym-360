'use client';

import { useState } from 'react';
import { cn } from '../lib/utils';
import { Calendar, ChevronDown } from 'lucide-react';
import { format, subDays, subMonths, startOfMonth, endOfMonth } from 'date-fns';

interface DateRange {
  from: Date;
  to: Date;
}

interface DateRangePickerProps {
  value?: DateRange;
  onChange: (range: DateRange) => void;
  className?: string;
}

const presets: { label: string; range: () => DateRange }[] = [
  { label: 'Last 7 days', range: () => ({ from: subDays(new Date(), 7), to: new Date() }) },
  { label: 'Last 30 days', range: () => ({ from: subDays(new Date(), 30), to: new Date() }) },
  { label: 'Last 90 days', range: () => ({ from: subDays(new Date(), 90), to: new Date() }) },
  { label: 'This month', range: () => ({ from: startOfMonth(new Date()), to: new Date() }) },
  {
    label: 'Last month',
    range: () => ({
      from: startOfMonth(subMonths(new Date(), 1)),
      to: endOfMonth(subMonths(new Date(), 1)),
    }),
  },
  { label: 'Last 6 months', range: () => ({ from: subMonths(new Date(), 6), to: new Date() }) },
  { label: 'Last year', range: () => ({ from: subMonths(new Date(), 12), to: new Date() }) },
];

export function DateRangePicker({ value, onChange, className }: DateRangePickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const displayValue = value
    ? `${format(value.from, 'MMM d, yyyy')} - ${format(value.to, 'MMM d, yyyy')}`
    : 'Select date range';

  return (
    <div className={cn('relative', className)}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 h-9 rounded-lg border border-input bg-background px-3 text-sm shadow-sm hover:bg-accent transition-colors"
      >
        <Calendar className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm">{displayValue}</span>
        <ChevronDown className="h-3.5 w-3.5 text-muted-foreground" />
      </button>

      {isOpen && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setIsOpen(false)} />
          <div className="absolute right-0 top-full mt-1 z-50 rounded-xl border bg-card shadow-lg p-3 w-56">
            <div className="space-y-1">
              {presets.map((preset) => (
                <button
                  key={preset.label}
                  onClick={() => {
                    onChange(preset.range());
                    setIsOpen(false);
                  }}
                  className="w-full text-left rounded-lg px-3 py-2 text-sm hover:bg-muted transition-colors"
                >
                  {preset.label}
                </button>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}

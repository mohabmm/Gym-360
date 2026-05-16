'use client';

import { cn } from '../lib/utils';

type BadgeVariant =
  | 'active' | 'inactive' | 'suspended' | 'pending' | 'terminated'
  | 'completed' | 'cancelled' | 'no_show' | 'late_cancel' | 'in_progress'
  | 'overdue' | 'warning' | 'current' | 'voided' | 'failed'
  | 'approved' | 'rejected' | 'expired' | 'frozen'
  | 'free' | 'starter' | 'pro' | 'business'
  | 'super_admin' | 'admin' | 'support' | 'analyst'
  | 'owner' | 'manager' | 'receptionist'
  | 'grace_period' | 'past_due'
  | 'default';

const variantClasses: Record<string, string> = {
  active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  inactive: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400',
  suspended: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  pending: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  pending_approval: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  terminated: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300',
  completed: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  cancelled: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  no_show: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
  late_cancel: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  in_progress: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  scheduled: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
  overdue: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  warning: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  current: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  voided: 'bg-gray-200 text-gray-600 dark:bg-gray-700 dark:text-gray-300',
  failed: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  approved: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  rejected: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  expired: 'bg-gray-200 text-gray-600 dark:bg-gray-700 dark:text-gray-300',
  frozen: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
  grace_period: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  past_due: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  free: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400',
  starter: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  pro: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  business: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  super_admin: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  admin: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  support: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  analyst: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  owner: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  manager: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  receptionist: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  default: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400',
};

interface StatusBadgeProps {
  variant: BadgeVariant | string;
  label?: string;
  className?: string;
  dot?: boolean;
}

export function StatusBadge({ variant, label, className, dot = false }: StatusBadgeProps) {
  const displayLabel = label ?? variant.replace(/_/g, ' ');
  const classes = variantClasses[variant] ?? variantClasses['default'];

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold capitalize whitespace-nowrap',
        classes,
        className
      )}
    >
      {dot && (
        <span
          className={cn(
            'h-1.5 w-1.5 rounded-full',
            variant === 'active' || variant === 'completed' || variant === 'current' || variant === 'approved'
              ? 'bg-emerald-500'
              : variant === 'suspended' || variant === 'terminated' || variant === 'overdue' || variant === 'failed' || variant === 'rejected'
                ? 'bg-red-500'
                : variant === 'pending' || variant === 'warning' || variant === 'grace_period'
                  ? 'bg-amber-500'
                  : 'bg-gray-400'
          )}
        />
      )}
      {displayLabel}
    </span>
  );
}

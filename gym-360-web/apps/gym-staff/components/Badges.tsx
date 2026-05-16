'use client';

import { cn } from '@gymapp/ui';

export function StatusBadge({ status }: { status: string }) {
  const config: Record<string, string> = {
    // Member statuses
    active: 'bg-red-50 text-red-700 border-red-200',
    expired: 'bg-slate-100 text-slate-700 border-slate-200',
    frozen: 'bg-blue-50 text-blue-700 border-blue-200',
    inactive: 'bg-slate-50 text-slate-600 border-slate-200',
    // Capitalized variants (Lovable data)
    Active: 'bg-red-50 text-red-700 border-red-200',
    Expired: 'bg-slate-100 text-slate-700 border-slate-200',
    Frozen: 'bg-blue-50 text-blue-700 border-blue-200',
    Published: 'bg-red-50 text-red-700 border-red-200',
    Scheduled: 'bg-amber-50 text-amber-700 border-amber-200',
    Draft: 'bg-slate-50 text-slate-600 border-slate-200',
    Exhausted: 'bg-slate-100 text-slate-700 border-slate-200',
    Low: 'bg-amber-50 text-amber-700 border-amber-200',
    Inactive: 'bg-slate-50 text-slate-600 border-slate-200',
    // Payment statuses
    completed: 'bg-green-50 text-green-700 border-green-200',
    pending: 'bg-amber-50 text-amber-700 border-amber-200',
    voided: 'bg-slate-100 text-slate-500 border-slate-200',
    failed: 'bg-red-50 text-red-700 border-red-200',
  };

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border",
      config[status] || 'bg-muted text-muted-foreground border-border'
    )}>
      {status}
    </span>
  );
}

export function MembershipTypeBadge({ type }: { type: string }) {
  const config: Record<string, string> = {
    monthly: 'bg-slate-50 text-slate-700 border-slate-200',
    quarterly: 'bg-purple-50 text-purple-700 border-purple-200',
    semi_annual: 'bg-blue-50 text-blue-700 border-blue-200',
    annual: 'bg-rose-50 text-rose-700 border-rose-200',
    session_package: 'bg-amber-50 text-amber-700 border-amber-200',
  };

  const labels: Record<string, string> = {
    monthly: 'Monthly',
    quarterly: 'Quarterly',
    semi_annual: '6 Months',
    annual: 'Annual',
    session_package: 'Sessions',
  };

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border",
      config[type] || 'bg-muted text-muted-foreground border-border'
    )}>
      {labels[type] || type}
    </span>
  );
}

export function MethodBadge({ method }: { method: string }) {
  const config: Record<string, string> = {
    cash: 'bg-red-50 text-red-700 border-red-200',
    Cash: 'bg-red-50 text-red-700 border-red-200',
    card: 'bg-blue-50 text-blue-700 border-blue-200',
    Card: 'bg-blue-50 text-blue-700 border-blue-200',
    vodafone_cash: 'bg-rose-50 text-rose-700 border-rose-200',
    instapay: 'bg-purple-50 text-purple-700 border-purple-200',
    bank_transfer: 'bg-amber-50 text-amber-700 border-amber-200',
    qr: 'bg-blue-50 text-blue-700 border-blue-200',
    QR: 'bg-blue-50 text-blue-700 border-blue-200',
    manual: 'bg-slate-50 text-slate-600 border-slate-200',
    Manual: 'bg-slate-50 text-slate-600 border-slate-200',
    numpad: 'bg-green-50 text-green-700 border-green-200',
  };

  const labels: Record<string, string> = {
    vodafone_cash: 'Vodafone Cash',
    bank_transfer: 'Bank Transfer',
  };

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border",
      config[method] || 'bg-muted text-muted-foreground border-border'
    )}>
      {labels[method] || method}
    </span>
  );
}

export function RoleBadge({ role }: { role: string }) {
  const config: Record<string, string> = {
    owner: 'bg-amber-50 text-amber-700 border-amber-200',
    Owner: 'bg-amber-50 text-amber-700 border-amber-200',
    manager: 'bg-blue-50 text-blue-700 border-blue-200',
    Manager: 'bg-blue-50 text-blue-700 border-blue-200',
    receptionist: 'bg-slate-50 text-slate-600 border-slate-200',
    Receptionist: 'bg-slate-50 text-slate-600 border-slate-200',
  };

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border",
      config[role] || 'bg-muted text-muted-foreground border-border'
    )}>
      {role}
    </span>
  );
}

export function CategoryBadge({ category }: { category: string }) {
  const config: Record<string, string> = {
    all: 'bg-blue-50 text-blue-700 border-blue-200',
    members: 'bg-purple-50 text-purple-700 border-purple-200',
    trainers: 'bg-green-50 text-green-700 border-green-200',
    General: 'bg-blue-50 text-blue-700 border-blue-200',
    Schedule: 'bg-purple-50 text-purple-700 border-purple-200',
    Offer: 'bg-red-50 text-red-700 border-red-200',
    Maintenance: 'bg-amber-50 text-amber-700 border-amber-200',
  };

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border",
      config[category] || 'bg-muted text-muted-foreground border-border'
    )}>
      {category}
    </span>
  );
}

export function TrainerStatusBadge({ status }: { status: string }) {
  const config: Record<string, string> = {
    active: 'bg-green-50 text-green-700 border-green-200',
    inactive: 'bg-slate-100 text-slate-600 border-slate-200',
    pending_approval: 'bg-amber-50 text-amber-700 border-amber-200',
    suspended: 'bg-red-50 text-red-700 border-red-200',
  };

  const labels: Record<string, string> = {
    pending_approval: 'Pending',
  };

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border",
      config[status] || 'bg-muted text-muted-foreground border-border'
    )}>
      {labels[status] || status}
    </span>
  );
}

'use client';

import { StatusBadge, type Gym } from '@gym360/ui';
import { Edit, Pause, Play, Trash2, AlertCircle } from 'lucide-react';
import { GYM_DETAIL_TABS, type GymDetailTab, type GymPlanSummary } from '@/lib/gym-detail';

export function ActionErrorBanner({ message, onDismiss }: { message: string | null; onDismiss: () => void }) {
  if (!message) return null;
  return (
    <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-3 dark:border-red-800 dark:bg-red-950/20">
      <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
      <p className="text-sm text-red-700 dark:text-red-400">{message}</p>
      <button onClick={onDismiss} className="ml-auto text-red-400 hover:text-red-600 text-lg leading-none">×</button>
    </div>
  );
}

interface GymHeaderActionsProps {
  status: Gym['status'];
  onEdit: () => void;
  onSuspend: () => void;
  onReactivate: () => void;
  onDelete: () => void;
}

export function GymHeaderActions({ status, onEdit, onSuspend, onReactivate, onDelete }: GymHeaderActionsProps) {
  return (
    <div className="flex items-center gap-2">
      <button
        onClick={onEdit}
        className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium hover:bg-accent"
      >
        <Edit className="h-4 w-4" /> Edit
      </button>
      {status === 'active' && (
        <button
          onClick={onSuspend}
          className="inline-flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100"
        >
          <Pause className="h-4 w-4" /> Suspend
        </button>
      )}
      {status === 'suspended' && (
        <button
          onClick={onReactivate}
          className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-700"
        >
          <Play className="h-4 w-4" /> Reactivate
        </button>
      )}
      <button
        onClick={onDelete}
        className="inline-flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 dark:border-red-800 dark:bg-red-950/30 dark:text-red-400"
      >
        <Trash2 className="h-4 w-4" /> Delete
      </button>
    </div>
  );
}

export function GymStatusBadges({ gym, summary }: { gym: Gym; summary: GymPlanSummary }) {
  return (
    <div className="flex items-center gap-3">
      <StatusBadge variant={gym.status} dot label={`Status: ${gym.status}`} />
      <StatusBadge variant={summary.planNameLower || 'inactive'} label={`Plan: ${summary.planLabel}`} />
      <StatusBadge variant={gym.paymentStatus} label={`Payment: ${gym.paymentStatus}`} />
    </div>
  );
}

const tabClass = (isActive: boolean) =>
  `pb-3 text-sm font-medium transition-colors border-b-2 ${
    isActive ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
  }`;

export function GymTabBar({ activeTab, onChange }: { activeTab: GymDetailTab; onChange: (tab: GymDetailTab) => void }) {
  return (
    <div className="border-b">
      <div className="flex gap-6">
        {GYM_DETAIL_TABS.map((tab) => (
          <button key={tab} onClick={() => onChange(tab)} className={tabClass(activeTab === tab)}>
            {tab}
          </button>
        ))}
      </div>
    </div>
  );
}

'use client';

import type { ReactNode } from 'react';
import { KpiCard, StatusBadge, formatCurrency, formatNumber, formatDate, type Gym } from '@gym360/ui';
import { Building2, Users, DollarSign, Dumbbell } from 'lucide-react';
import type { GymPlanSummary } from '@/lib/gym-detail';

function DetailItem({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div><dt className="text-muted-foreground">{label}</dt>{children}</div>
  );
}

function QuickStatRow({ label, value, last = false }: { label: string; value: ReactNode; last?: boolean }) {
  return (
    <div className={last ? 'flex items-center justify-between py-2' : 'flex items-center justify-between py-2 border-b'}>
      <span className="text-sm text-muted-foreground">{label}</span>
      <span className="text-sm font-medium">{value}</span>
    </div>
  );
}

export function GymOverviewTab({ gym, summary }: { gym: Gym; summary: GymPlanSummary }) {
  const { planNameLower, planLabel, ownerName, plan } = summary;
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard value={formatNumber(gym.activeMemberCount)} label="Active Members" icon={<Users className="h-5 w-5" />} variant="success" />
        <KpiCard value={formatNumber(gym.activeTrainerCount)} label="Active Trainers" icon={<Dumbbell className="h-5 w-5" />} variant="info" />
        <KpiCard value={formatCurrency(gym.monthlyRevenue ?? 0)} label="Monthly Revenue" icon={<DollarSign className="h-5 w-5" />} />
        <KpiCard value={gym.subscriptionStatus || '—'} label="Subscription Status" icon={<Building2 className="h-5 w-5" />} />
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Gym Details</h3>
          <dl className="grid grid-cols-2 gap-4 text-sm">
            <DetailItem label="Gym Code"><dd className="font-medium">{gym.gymCode}</dd></DetailItem>
            <DetailItem label="City"><dd className="font-medium">{gym.address?.city ?? gym.gymCode}</dd></DetailItem>
            <DetailItem label="Owner"><dd className="font-medium">{ownerName}</dd></DetailItem>
            <DetailItem label="Joined"><dd className="font-medium">{formatDate(gym.createdAt)}</dd></DetailItem>
            <DetailItem label="Plan"><dd><StatusBadge variant={planNameLower || 'inactive'} label={planLabel} /></dd></DetailItem>
            <DetailItem label="Payment"><dd><StatusBadge variant={gym.paymentStatus} /></dd></DetailItem>
          </dl>
        </div>
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Quick Stats</h3>
          <div className="space-y-3">
            <QuickStatRow label="Active Members" value={gym.activeMemberCount} />
            <QuickStatRow label="Active Trainers" value={gym.activeTrainerCount} />
            <QuickStatRow label="Monthly Revenue" value={formatCurrency(gym.monthlyRevenue ?? 0)} />
            <QuickStatRow label="Active Subscription" value={plan?.name ?? '—'} last />
          </div>
        </div>
      </div>
    </div>
  );
}

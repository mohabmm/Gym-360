import type { ReactNode } from 'react';
import { StatusBadge, CardSkeleton, formatCurrency, type GymListItem, type SubscriptionPlan } from '@gym360/ui';
import { Edit } from 'lucide-react';
import { tierOf } from '@/lib/plans';

export type PlanWithGymStats = SubscriptionPlan & { gymCount: number; totalRevenue: number };

/** Attach per-plan gym count + summed monthly revenue. */
export function withGymStats(plans: SubscriptionPlan[], gyms: GymListItem[]): PlanWithGymStats[] {
  return plans.map((plan) => {
    const planGyms = gyms.filter((g) => g.planName === plan.name);
    return {
      ...plan,
      gymCount: planGyms.length,
      totalRevenue: planGyms.reduce((sum, g) => sum + g.monthlyRevenue, 0),
    };
  });
}

function StatRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  );
}

function PlanPrice({ monthlyPriceEgp }: { monthlyPriceEgp: number }) {
  return (
    <p className="text-2xl font-bold mt-2">
      {monthlyPriceEgp === 0 ? 'Free' : formatCurrency(monthlyPriceEgp)}
      {monthlyPriceEgp > 0 && <span className="text-sm font-normal text-muted-foreground">/mo</span>}
    </p>
  );
}

function SubscriptionPlanCard({ plan, onEdit }: { plan: PlanWithGymStats; onEdit: (plan: SubscriptionPlan) => void }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-center justify-between mb-4">
        <StatusBadge variant={tierOf(plan)} className="text-sm px-3 py-1" />
        <button
          onClick={() => onEdit(plan)}
          className="p-1.5 rounded-lg hover:bg-muted transition-colors text-muted-foreground"
        >
          <Edit className="h-4 w-4" />
        </button>
      </div>
      <h3 className="text-xl font-bold">{plan.name}</h3>
      <PlanPrice monthlyPriceEgp={plan.monthlyPriceEgp} />
      <div className="mt-4 space-y-2 text-sm">
        <StatRow label="Gyms" value={plan.gymCount} />
        <StatRow label="Revenue" value={formatCurrency(plan.totalRevenue)} />
        <StatRow label="Max Members" value={plan.memberLimit} />
        <StatRow label="Max Trainers" value={plan.trainerLimit} />
        <StatRow label="Max Staff" value={plan.staffLimit} />
      </div>
    </div>
  );
}

interface PlanCardsGridProps {
  isLoading: boolean;
  plans: PlanWithGymStats[];
  onEdit: (plan: SubscriptionPlan) => void;
}

export function PlanCardsGrid({ isLoading, plans, onEdit }: PlanCardsGridProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      {isLoading
        ? Array.from({ length: 4 }).map((_, i) => <CardSkeleton key={i} />)
        : plans.map((plan) => <SubscriptionPlanCard key={plan.id} plan={plan} onEdit={onEdit} />)}
    </div>
  );
}

const TH = 'py-2.5 px-4 font-semibold text-muted-foreground';

export function ActiveSubscriptionsTable({ gyms }: { gyms: GymListItem[] }) {
  return (
    <div className="rounded-xl border bg-card shadow-sm">
      <div className="p-4 border-b">
        <h3 className="text-sm font-semibold">Active Subscriptions</h3>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className={`text-left ${TH}`}>Gym</th>
              <th className={`text-left ${TH}`}>Plan</th>
              <th className={`text-left ${TH}`}>Status</th>
              <th className={`text-right ${TH}`}>Monthly</th>
              <th className={`text-left ${TH}`}>Payment</th>
            </tr>
          </thead>
          <tbody>
            {gyms.filter((g) => g.status !== 'terminated').map((gym) => (
              <tr key={gym.id} className="border-b last:border-0 hover:bg-muted/50">
                <td className="py-2.5 px-4">
                  <p className="font-medium">{gym.name}</p>
                  <p className="text-xs text-muted-foreground">{gym.city}</p>
                </td>
                <td className="py-2.5 px-4"><StatusBadge variant={(gym.planName ?? '').toLowerCase()} /></td>
                <td className="py-2.5 px-4"><StatusBadge variant={gym.status} dot /></td>
                <td className="py-2.5 px-4 text-right font-medium">{formatCurrency(gym.monthlyRevenue)}</td>
                <td className="py-2.5 px-4"><StatusBadge variant={gym.paymentStatus} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

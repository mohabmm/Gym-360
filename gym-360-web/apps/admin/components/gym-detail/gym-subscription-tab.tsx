'use client';

import { StatusBadge, formatCurrency, type Gym, type SubscriptionPlan } from '@gym360/ui';
import {
  gymStaffCount,
  planDefaultFor,
  pluralSuffix,
  usagePercent,
  type FeatureMatrixLookup,
  type GymPlanSummary,
} from '@/lib/gym-detail';
import { useFeatureOverrideToggle } from './use-gym-detail-actions';

interface FeatureRef {
  id: string;
  code: string;
  name: string;
}

interface GymOverrideRef {
  id: string;
  featureId: string;
  overrideValue: string;
}

interface UsageBarProps {
  label: string;
  used: number;
  display: number | string;
  limit: number;
  barClass: string;
}

function UsageBar({ label, used, display, limit, barClass }: UsageBarProps) {
  return (
    <div>
      <p className="text-xs text-muted-foreground mb-1">{label}</p>
      <div className="flex items-center gap-2">
        <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
          <div className={`h-full ${barClass} rounded-full`} style={{ width: `${usagePercent(used, limit)}%` }} />
        </div>
        <span className="text-xs font-medium">{display}/{limit}</span>
      </div>
    </div>
  );
}

function PlanUsageGrid({ gym, plan }: { gym: Gym; plan: SubscriptionPlan }) {
  const staffCount = gymStaffCount(gym);
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      <UsageBar label="Members" used={gym.activeMemberCount} display={gym.activeMemberCount} limit={plan.memberLimit} barClass="bg-emerald-500" />
      <UsageBar label="Trainers" used={gym.activeTrainerCount} display={gym.activeTrainerCount} limit={plan.trainerLimit} barClass="bg-blue-500" />
      <UsageBar label="Staff" used={staffCount ?? 0} display={staffCount ?? '—'} limit={plan.staffLimit} barClass="bg-amber-500" />
      <div>
        <p className="text-xs text-muted-foreground mb-1">Plan Tier</p>
        <div className="flex items-center gap-2">
          <span className="text-xs font-medium uppercase">{plan.code}</span>
        </div>
      </div>
    </div>
  );
}

function CurrentPlanCard({ gym, summary, onChangePlan }: { gym: Gym; summary: GymPlanSummary; onChangePlan: () => void }) {
  const { plan, planNameLower, planLabel } = summary;
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold">Current Plan</h3>
        <button
          onClick={onChangePlan}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90"
        >
          Change Plan
        </button>
      </div>
      <div className="flex items-center gap-4 mb-4">
        <StatusBadge variant={planNameLower || 'inactive'} label={planLabel} className="text-base px-4 py-1" />
        <span className="text-lg font-bold">{formatCurrency(plan?.monthlyPriceEgp ?? 0)}/mo</span>
      </div>
      {plan && <PlanUsageGrid gym={gym} plan={plan} />}
    </div>
  );
}

function OverrideCell({ overrideValue }: { overrideValue: string | undefined }) {
  if (!overrideValue) return <span className="text-xs text-muted-foreground">Plan Default</span>;
  const forcedOn = overrideValue === 'force_on';
  return <StatusBadge variant={forcedOn ? 'active' : 'suspended'} label={forcedOn ? 'Force ON' : 'Force OFF'} />;
}

function overrideActionLabel(isBusy: boolean, hasOverride: boolean): string {
  if (isBusy) return '...';
  return hasOverride ? 'Remove Override' : 'Add Override';
}

interface FeatureOverridesCardProps {
  gymId: string;
  plan: SubscriptionPlan | undefined;
  features: FeatureRef[];
  featureMatrix: FeatureMatrixLookup;
  gymOverrides: GymOverrideRef[];
}

function FeatureOverridesCard({ gymId, plan, features, featureMatrix, gymOverrides }: FeatureOverridesCardProps) {
  const { busyOverrideCode, toggleOverride } = useFeatureOverrideToggle(gymId);

  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold">Feature Overrides</h3>
        <span className="text-xs text-muted-foreground">
          {features.length} features · {gymOverrides.length} override{pluralSuffix(gymOverrides.length)}
        </span>
      </div>
      <div className="overflow-x-auto overflow-y-auto max-h-[32rem]">
        <table className="w-full text-sm">
          <thead className="sticky top-0 bg-card">
            <tr className="border-b bg-muted/50">
              <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Feature</th>
              <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Plan Default</th>
              <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Override</th>
              <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Actions</th>
            </tr>
          </thead>
          <tbody>
            {features.map((feature) => {
              const planDefault = planDefaultFor(featureMatrix, plan, feature.id);
              const override = gymOverrides.find((o) => o.featureId === feature.id);
              const isBusy = busyOverrideCode === feature.code;
              return (
                <tr key={feature.id} className="border-b last:border-0 hover:bg-muted/50">
                  <td className="py-2 px-3 font-medium">{feature.name}</td>
                  <td className="py-2 px-3">
                    <StatusBadge variant={planDefault ? 'active' : 'inactive'} label={planDefault ? 'ON' : 'OFF'} />
                  </td>
                  <td className="py-2 px-3">
                    <OverrideCell overrideValue={override?.overrideValue} />
                  </td>
                  <td className="py-2 px-3">
                    <button
                      onClick={() =>
                        toggleOverride({ featureId: feature.id, featureCode: feature.code, existingOverrideId: override?.id, planDefault })
                      }
                      disabled={isBusy}
                      className="text-xs text-primary hover:underline disabled:opacity-50"
                    >
                      {overrideActionLabel(isBusy, Boolean(override))}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

interface GymSubscriptionTabProps {
  gymId: string;
  gym: Gym;
  summary: GymPlanSummary;
  features: FeatureRef[];
  featureMatrix: FeatureMatrixLookup;
  gymOverrides: GymOverrideRef[];
  onChangePlan: () => void;
}

export function GymSubscriptionTab({ gymId, gym, summary, onChangePlan, ...matrix }: GymSubscriptionTabProps) {
  return (
    <div className="space-y-6">
      <CurrentPlanCard gym={gym} summary={summary} onChangePlan={onChangePlan} />
      <FeatureOverridesCard gymId={gymId} plan={summary.plan} {...matrix} />
    </div>
  );
}

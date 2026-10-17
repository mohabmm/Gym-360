'use client';

import { PageHeader } from '@gym360/ui';
import { Calendar, AlertCircle } from 'lucide-react';
import { useMySubscription, useMyPlanFeatures, type PlanFeatureSet } from '@/lib/hooks/useBilling';
import { CurrentPlanCard, PlanLimits, PlanFeatures } from '@/components/subscription/SubscriptionSections';

// Missing plan data reads as "no features, zero limits".
function planLimits(features: PlanFeatureSet | null | undefined) {
  return {
    enabledFeatures: features?.enabledFeatures ?? [],
    memberLimit: features?.memberLimit ?? 0,
    trainerLimit: features?.trainerLimit ?? 0,
    staffLimit: features?.staffLimit ?? 0,
  };
}

function NoSubscription() {
  return (
    <div className="space-y-6">
      <PageHeader title="My Subscription" description="Active plan, features and renewal status" />
      <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-4 dark:border-amber-800 dark:bg-amber-950/20">
        <AlertCircle className="h-5 w-5 text-amber-500 shrink-0" />
        <p className="text-sm text-amber-700 dark:text-amber-400">
          No active subscription found for this gym. Contact platform admin to activate a plan.
        </p>
      </div>
    </div>
  );
}

export default function SubscriptionPage() {
  const { data: sub, isLoading: subLoading, isError: subError } = useMySubscription();
  const { data: features, isLoading: featLoading } = useMyPlanFeatures();

  if (subLoading || featLoading) {
    return (
      <div className="space-y-6">
        <PageHeader title="My Subscription" description="Loading..." />
      </div>
    );
  }

  if (subError || !sub) return <NoSubscription />;

  const { enabledFeatures, ...limits } = planLimits(features);

  return (
    <div className="space-y-6">
      <PageHeader
        title="My Subscription"
        description="Active plan, features and renewal status for your gym"
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Current Plan Card */}
        <CurrentPlanCard sub={sub} />

        {/* KPIs */}
        <PlanLimits {...limits} />
      </div>

      {/* Enabled Features */}
      <PlanFeatures enabledFeatures={enabledFeatures} />

      <div className="rounded-xl border border-blue-200 bg-blue-50 dark:border-blue-800 dark:bg-blue-950/20 p-4 text-sm text-blue-700 dark:text-blue-400">
        <div className="flex items-start gap-2">
          <Calendar className="h-4 w-4 shrink-0 mt-0.5" />
          <p>
            Need to upgrade or change your plan? Contact platform support — plan changes are managed by Gym 360 admin.
          </p>
        </div>
      </div>
    </div>
  );
}

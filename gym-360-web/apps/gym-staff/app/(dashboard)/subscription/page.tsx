'use client';

import { PageHeader, StatusBadge, KpiCard, formatDate } from '@gymapp/ui';
import { CreditCard, CheckCircle2, Users, Dumbbell, UserCheck, Calendar, AlertCircle } from 'lucide-react';
import { useMySubscription, useMyPlanFeatures } from '@/lib/hooks/useBilling';

const FEATURE_LABELS: Record<string, string> = {
  session_packages: 'Session Packages',
  trainer_earnings: 'Trainer Earnings',
  data_import: 'Bulk Data Import',
  remote_workouts: 'Remote Workouts',
  branding: 'Custom Branding',
  peak_hours_analytics: 'Peak Hours Analytics',
  testimonials: 'Member Testimonials',
  trainer_leaderboard: 'Trainer Leaderboard',
  trainer_ratings: 'Trainer Ratings',
  data_export: 'Data Export',
  classes: 'Classes',
  programs: 'Workout Programs',
  exercises: 'Exercise Library',
  meal_plans: 'Meal Plans',
  progress_photos: 'Progress Photos',
  body_metrics: 'Body Metrics',
  announcements: 'Announcements',
};

function humanFeature(code: string): string {
  return FEATURE_LABELS[code] ?? code.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
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

  if (subError || !sub) {
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

  const enabledFeatures = features?.enabledFeatures ?? [];
  const memberLimit = features?.memberLimit ?? 0;
  const trainerLimit = features?.trainerLimit ?? 0;
  const staffLimit = features?.staffLimit ?? 0;

  return (
    <div className="space-y-6">
      <PageHeader
        title="My Subscription"
        description="Active plan, features and renewal status for your gym"
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Current Plan Card */}
        <div className="lg:col-span-2 rounded-xl border bg-card p-6 shadow-sm">
          <div className="flex items-start justify-between mb-4">
            <div>
              <p className="text-xs uppercase text-muted-foreground tracking-wide">Current Plan</p>
              <h2 className="text-2xl font-bold mt-1">{sub.planName}</h2>
              <p className="text-sm text-muted-foreground mt-1">Code: <span className="font-mono">{sub.planCode}</span></p>
            </div>
            <div className="flex flex-col items-end gap-2">
              <StatusBadge variant={sub.isActive ? 'active' : 'inactive'} dot />
              <StatusBadge variant={sub.paymentStatus.toLowerCase() === 'paid' ? 'active' : 'overdue'} label={sub.paymentStatus} />
              {sub.isTrial && <StatusBadge variant="warning" label="Trial" />}
            </div>
          </div>

          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
            <div>
              <dt className="text-muted-foreground">Started</dt>
              <dd className="font-medium">{formatDate(sub.startedAt)}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Expires</dt>
              <dd className="font-medium">{formatDate(sub.expiresAt)}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Last Payment</dt>
              <dd className="font-medium">{sub.lastPaymentAt ? formatDate(sub.lastPaymentAt) : '—'}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Next Payment Due</dt>
              <dd className="font-medium">{sub.nextPaymentDue ? formatDate(sub.nextPaymentDue) : '—'}</dd>
            </div>
            {sub.overdueSince && (
              <div className="sm:col-span-2 rounded-lg bg-red-50 dark:bg-red-950/20 px-3 py-2">
                <dt className="text-red-700 dark:text-red-400 text-xs uppercase">Overdue Since</dt>
                <dd className="font-medium text-red-700 dark:text-red-400">{formatDate(sub.overdueSince)}</dd>
              </div>
            )}
          </dl>
        </div>

        {/* KPIs */}
        <div className="space-y-4">
          <KpiCard
            value={memberLimit < 0 ? 'Unlimited' : memberLimit.toString()}
            label="Member Limit"
            icon={<Users className="h-5 w-5" />}
          />
          <KpiCard
            value={trainerLimit < 0 ? 'Unlimited' : trainerLimit.toString()}
            label="Trainer Limit"
            icon={<UserCheck className="h-5 w-5" />}
          />
          <KpiCard
            value={staffLimit < 0 ? 'Unlimited' : staffLimit.toString()}
            label="Staff Limit"
            icon={<Dumbbell className="h-5 w-5" />}
          />
        </div>
      </div>

      {/* Enabled Features */}
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-base font-semibold">Plan Features</h3>
            <p className="text-xs text-muted-foreground mt-1">
              {enabledFeatures.length} feature{enabledFeatures.length === 1 ? '' : 's'} enabled on this plan
            </p>
          </div>
          <CreditCard className="h-5 w-5 text-muted-foreground" />
        </div>

        {enabledFeatures.length === 0 ? (
          <p className="text-sm text-muted-foreground">No premium features on the current plan.</p>
        ) : (
          <ul className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {enabledFeatures.map((code) => (
              <li
                key={code}
                className="flex items-center gap-2 rounded-lg border bg-muted/30 px-3 py-2 text-sm"
              >
                <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
                <span>{humanFeature(code)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>

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

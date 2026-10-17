import { StatusBadge, DataTable } from '@gym360/ui';
import type { PlanChangeLog, SubscriptionPlan, Feature } from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Rocket, Plus, Edit, Trash2, AlertCircle } from 'lucide-react';
import { tierOf, matrixCellKey } from '@/lib/plans';

function PlanNameBadge({ name }: { name?: string | null }) {
  return name ? <StatusBadge variant={name.toLowerCase()} /> : <span className="text-xs text-muted-foreground">—</span>;
}

const overrideColumns: ColumnDef<PlanChangeLog, unknown>[] = [
  { accessorKey: 'gymName', header: 'Gym' },
  { accessorKey: 'previousPlanName', header: 'Previous Plan', cell: ({ row }) => <PlanNameBadge name={row.original.previousPlanName} /> },
  { accessorKey: 'newPlanName', header: 'New Plan', cell: ({ row }) => <PlanNameBadge name={row.original.newPlanName} /> },
  { accessorKey: 'changedByName', header: 'Changed By' },
  { accessorKey: 'reason', header: 'Reason', cell: ({ row }) => row.original.reason ?? '-' },
  { accessorKey: 'changedAt', header: 'Date', cell: ({ row }) => new Date(row.original.changedAt).toLocaleDateString() },
];

export function PlanChangeLogSection({ logs }: { logs: PlanChangeLog[] }) {
  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Plan Change Audit Log</h2>
      <DataTable data={logs} columns={overrideColumns} pageSize={5} />
    </div>
  );
}

interface DeployProps {
  pendingChanges: number;
  onDeploy: () => void;
}

export function DeployHeaderButton({ pendingChanges, onDeploy }: DeployProps) {
  return (
    <div className="flex items-center gap-2">
      {pendingChanges > 0 && (
        <button
          onClick={onDeploy}
          className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 animate-pulse"
        >
          <Rocket className="h-4 w-4" />
          Deploy Changes
          <span className="bg-white/20 text-white text-xs px-1.5 py-0.5 rounded-full">{pendingChanges}</span>
        </button>
      )}
    </div>
  );
}

/** Sticky Deploy Bar */
export function DeployBar({ pendingChanges, onDeploy }: DeployProps) {
  if (pendingChanges <= 0) return null;
  return (
    <div className="sticky top-0 z-10 flex items-center gap-3 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 dark:border-emerald-800 dark:bg-emerald-950/20">
      <AlertCircle className="h-5 w-5 text-emerald-600 shrink-0" />
      <p className="text-sm font-medium text-emerald-800 dark:text-emerald-300">
        {pendingChanges} pending changes need to be deployed to take effect
      </p>
      <button
        onClick={onDeploy}
        className="ml-auto inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700"
      >
        <Rocket className="h-3.5 w-3.5" /> Deploy Now
      </button>
    </div>
  );
}

interface SectionHeaderProps {
  title: string;
  actionLabel: string;
  onAction: () => void;
}

function SectionHeader({ title, actionLabel, onAction }: SectionHeaderProps) {
  return (
    <div className="flex items-center justify-between mb-4">
      <h2 className="text-lg font-semibold">{title}</h2>
      <button
        onClick={onAction}
        className="inline-flex items-center gap-2 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent"
      >
        <Plus className="h-4 w-4" /> {actionLabel}
      </button>
    </div>
  );
}

interface PlanCardProps {
  plan: SubscriptionPlan;
  onEdit: (plan: SubscriptionPlan) => void;
  onDelete: (plan: SubscriptionPlan) => void;
}

function PlanCard({ plan, onEdit, onDelete }: PlanCardProps) {
  return (
    <div className="rounded-xl border bg-card p-5 shadow-sm">
      <div className="flex items-center justify-between mb-3">
        <StatusBadge variant={tierOf(plan)} className="text-sm px-3 py-1" />
        <div className="flex gap-1">
          <button
            onClick={() => onEdit(plan)}
            aria-label={`Edit ${plan.name}`}
            className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground"
          >
            <Edit className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={() => onDelete(plan)}
            aria-label={`Delete ${plan.name}`}
            className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground hover:text-red-500"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
      <p className="text-xl font-bold">{plan.name}</p>
      <p className="text-sm text-muted-foreground mt-1">
        {plan.monthlyPriceEgp === 0 ? 'Free' : `EGP ${plan.monthlyPriceEgp}/mo`}
      </p>
    </div>
  );
}

interface PlansSectionProps {
  plans: SubscriptionPlan[];
  onCreate: () => void;
  onEdit: (plan: SubscriptionPlan) => void;
  onDelete: (plan: SubscriptionPlan) => void;
}

/** Plans Management */
export function PlansSection({ plans, onCreate, onEdit, onDelete }: PlansSectionProps) {
  return (
    <div>
      <SectionHeader title="Plans" actionLabel="Create Plan" onAction={onCreate} />
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {plans.map((plan) => (
          <PlanCard key={plan.id} plan={plan} onEdit={onEdit} onDelete={onDelete} />
        ))}
      </div>
    </div>
  );
}

const TH_LEFT = 'text-left py-2.5 px-4 font-semibold text-muted-foreground';

function FeatureRow({ feature, onEdit }: { feature: Feature; onEdit: (feature: Feature) => void }) {
  return (
    <tr className="border-b last:border-0 hover:bg-muted/50">
      <td className="py-2 px-4 font-medium">{feature.name}</td>
      <td className="py-2 px-4"><code className="text-xs bg-muted px-1.5 py-0.5 rounded">{feature.code}</code></td>
      <td className="py-2 px-4 text-muted-foreground">{feature.category}</td>
      <td className="py-2 px-4 text-center"><StatusBadge variant={feature.isActive ? 'active' : 'inactive'} /></td>
      <td className="py-2 px-4 text-right">
        <button
          onClick={() => onEdit(feature)}
          aria-label={`Edit ${feature.name}`}
          className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground"
        >
          <Edit className="h-3.5 w-3.5" />
        </button>
      </td>
    </tr>
  );
}

interface FeaturesSectionProps {
  features: Feature[];
  onAdd: () => void;
  onEdit: (feature: Feature) => void;
}

/** Features Management */
export function FeaturesSection({ features, onAdd, onEdit }: FeaturesSectionProps) {
  return (
    <div>
      <SectionHeader title={`Features (${features.length})`} actionLabel="Add Feature" onAction={onAdd} />
      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className={TH_LEFT}>Feature</th>
                <th className={TH_LEFT}>Code</th>
                <th className={TH_LEFT}>Category</th>
                <th className="text-center py-2.5 px-4 font-semibold text-muted-foreground">Status</th>
                <th className="py-2.5 px-4"></th>
              </tr>
            </thead>
            <tbody>
              {features.map((feature) => (
                <FeatureRow key={feature.id} feature={feature} onEdit={onEdit} />
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

function MatrixIcon({ enabled }: { enabled: boolean }) {
  if (enabled) {
    return (
      <div className="h-5 w-5 rounded bg-emerald-100 flex items-center justify-center mx-auto">
        <svg className="h-3 w-3 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
      </div>
    );
  }
  return (
    <div className="h-5 w-5 rounded bg-gray-100 flex items-center justify-center mx-auto dark:bg-gray-800">
      <svg className="h-3 w-3 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
    </div>
  );
}

export type ToggleMatrixHandler = (planId: string, featureId: string, currentlyEnabled: boolean) => void;

interface FeatureMatrixSectionProps {
  plans: SubscriptionPlan[];
  features: Feature[];
  featureMatrix: Record<string, Record<string, boolean>>;
  togglingCell: string | null;
  onToggle: ToggleMatrixHandler;
}

interface MatrixRowProps extends Omit<FeatureMatrixSectionProps, 'features'> {
  feature: Feature;
}

function MatrixRow({ feature, plans, featureMatrix, togglingCell, onToggle }: MatrixRowProps) {
  return (
    <tr className="border-b last:border-0 hover:bg-muted/50">
      <td className="py-2 px-4 font-medium sticky left-0 bg-card">{feature.name}</td>
      {plans.map(({ id: planId }) => {
        const enabled = featureMatrix[planId]?.[feature.id] ?? false;
        return (
          <td key={planId} className="py-2 px-4 text-center">
            <button
              onClick={() => onToggle(planId, feature.id, enabled)}
              disabled={togglingCell === matrixCellKey(planId, feature.id)}
              aria-label={`Toggle ${feature.name}`}
              className="mx-auto disabled:opacity-50"
            >
              <MatrixIcon enabled={enabled} />
            </button>
          </td>
        );
      })}
    </tr>
  );
}

/** Feature Matrix: rows = features, cols = plans */
export function FeatureMatrixSection({ features, ...rowProps }: FeatureMatrixSectionProps) {
  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Feature Matrix</h2>
      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground sticky left-0 bg-muted/50 min-w-[200px]">Feature</th>
                {rowProps.plans.map((plan) => (
                  <th key={plan.id} className="text-center py-2.5 px-4 font-semibold text-muted-foreground min-w-[100px]">
                    <StatusBadge variant={tierOf(plan)} />
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {features.map((feature) => (
                <MatrixRow key={feature.id} feature={feature} {...rowProps} />
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

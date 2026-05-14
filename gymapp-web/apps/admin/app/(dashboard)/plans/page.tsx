'use client';

import { useState } from 'react';
import {
  PageHeader,
  StatusBadge,
  Modal,
  DataTable,
  ConfirmModal,
  apiPost,
  apiPatch,
  apiPut,
  apiDelete,
  BILLING,
  useInvalidateQueries,
} from '@gymapp/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Rocket, Plus, Edit, Trash2, AlertCircle } from 'lucide-react';
import {
  usePlans,
  useFeatures,
  usePlanFeatureMatrix,
  usePlanChangeLogs,
  useDeploy,
  useTogglePlanFeature,
} from '@/lib/hooks';
import { TableSkeleton } from '@gymapp/ui';
import type { PlanChangeLog, SubscriptionPlan, Feature } from '@gymapp/ui';

export default function PlansPage() {
  const [pendingChanges, setPendingChanges] = useState(0);
  const [showDeployModal, setShowDeployModal] = useState(false);
  const [showFeatureModal, setShowFeatureModal] = useState(false);
  const [showPlanModal, setShowPlanModal] = useState(false);
  const emptyPlanForm = {
    code: '',
    name: '',
    description: '',
    monthlyPriceEgp: 0,
    annualPriceEgp: 0,
    memberLimit: 100,
    trainerLimit: 5,
    staffLimit: 3,
    isPublic: true,
    isActive: true,
    sortOrder: 0,
  };
  const [planForm, setPlanForm] = useState<typeof emptyPlanForm>(emptyPlanForm);
  const [editingPlanId, setEditingPlanId] = useState<string | null>(null);
  const [deletePlan, setDeletePlan] = useState<SubscriptionPlan | null>(null);
  const [editingFeature, setEditingFeature] = useState<Feature | null>(null);
  const [featureForm, setFeatureForm] = useState({ name: '', code: '', category: 'Training', isActive: true });
  const [createFeatureForm, setCreateFeatureForm] = useState({ name: '', code: '', category: 'Training' });
  const [busy, setBusy] = useState(false);
  const [togglingCell, setTogglingCell] = useState<string | null>(null);
  const { invalidate } = useInvalidateQueries();
  const togglePlanFeature = useTogglePlanFeature();

  const { data: plansData, isLoading: plansLoading, isError: plansError } = usePlans();
  const { data: featuresData, isLoading: featuresLoading, isError: featuresError } = useFeatures();
  const { data: matrixData } = usePlanFeatureMatrix();
  const { data: changeLogsData, isLoading: logsLoading } = usePlanChangeLogs();
  const deployMutation = useDeploy();

  // Backend GetFeatureMatrix returns { plans, features, matrix }. Prefer that shape when it's
  // available so we don't need two separate calls; fall back to the standalone lists otherwise.
  const matrix = matrixData?.data;
  const subscriptionPlans = matrix?.plans ?? plansData?.data ?? [];
  const features = matrix?.features ?? featuresData?.data ?? [];
  const planChangeLogs = changeLogsData?.data?.items ?? [];

  // Build matrix lookup: planId -> featureId -> isEnabled
  const featureMatrix = (matrix?.matrix ?? []).reduce<Record<string, Record<string, boolean>>>(
    (acc, pf) => {
      if (!acc[pf.planId]) acc[pf.planId] = {};
      acc[pf.planId]![pf.featureId] = pf.isEnabled;
      return acc;
    },
    {}
  );

  // Derive a display tier from the plan code (backend has no tier column).
  const tierOf = (plan: SubscriptionPlan): string => {
    const c = plan.code?.toLowerCase() ?? '';
    if (c.includes('free')) return 'free';
    if (c.includes('business') || c.includes('enterprise')) return 'business';
    if (c.includes('pro')) return 'pro';
    return 'starter';
  };

  // Feature Matrix: rows = features, cols = plans
  const planIds = subscriptionPlans.map((p) => p.id);

  const overrideColumns: ColumnDef<PlanChangeLog, unknown>[] = [
    { accessorKey: 'gymName', header: 'Gym' },
    { accessorKey: 'previousPlanName', header: 'Previous Plan', cell: ({ row }) => row.original.previousPlanName ? <StatusBadge variant={row.original.previousPlanName.toLowerCase()} /> : <span className="text-xs text-muted-foreground">—</span> },
    { accessorKey: 'newPlanName', header: 'New Plan', cell: ({ row }) => row.original.newPlanName ? <StatusBadge variant={row.original.newPlanName.toLowerCase()} /> : <span className="text-xs text-muted-foreground">—</span> },
    { accessorKey: 'changedByName', header: 'Changed By' },
    { accessorKey: 'reason', header: 'Reason', cell: ({ row }) => row.original.reason ?? '-' },
    { accessorKey: 'changedAt', header: 'Date', cell: ({ row }) => new Date(row.original.changedAt).toLocaleDateString() },
  ];

  const openCreatePlan = () => {
    setEditingPlanId(null);
    setPlanForm(emptyPlanForm);
    setShowPlanModal(true);
  };

  const openEditPlan = (p: SubscriptionPlan) => {
    setEditingPlanId(p.id);
    setPlanForm({
      code: p.code ?? '',
      name: p.name,
      description: p.description ?? '',
      monthlyPriceEgp: p.monthlyPriceEgp ?? 0,
      annualPriceEgp: p.annualPriceEgp ?? 0,
      memberLimit: p.memberLimit ?? 0,
      trainerLimit: p.trainerLimit ?? 0,
      staffLimit: p.staffLimit ?? 0,
      isPublic: p.isPublic ?? true,
      isActive: p.isActive ?? true,
      sortOrder: p.sortOrder ?? 0,
    });
    setShowPlanModal(true);
  };

  const handleSavePlan = async () => {
    if (!planForm.name?.trim() || !planForm.code?.trim()) return;
    setBusy(true);
    try {
      if (editingPlanId) {
        // PUT /admin/plans/{id} — all fields optional; backend ignores Code on update.
        await apiPut(BILLING.PLAN(editingPlanId), {
          name: planForm.name,
          description: planForm.description || null,
          monthlyPrice: planForm.monthlyPriceEgp,
          annualPrice: planForm.annualPriceEgp,
          memberLimit: planForm.memberLimit,
          trainerLimit: planForm.trainerLimit,
          staffLimit: planForm.staffLimit,
          isPublic: planForm.isPublic,
          isActive: planForm.isActive,
          sortOrder: planForm.sortOrder,
        });
      } else {
        await apiPost(BILLING.PLANS, {
          code: planForm.code,
          name: planForm.name,
          description: planForm.description || null,
          monthlyPrice: planForm.monthlyPriceEgp,
          annualPrice: planForm.annualPriceEgp,
          memberLimit: planForm.memberLimit,
          trainerLimit: planForm.trainerLimit,
          staffLimit: planForm.staffLimit,
          isPublic: planForm.isPublic,
          sortOrder: planForm.sortOrder,
        });
      }
      invalidate(['plans']);
      invalidate(['plan-features']);
      setShowPlanModal(false);
    } finally {
      setBusy(false);
    }
  };

  const handleDeletePlan = async () => {
    if (!deletePlan) return;
    setBusy(true);
    try {
      await apiDelete(BILLING.PLAN(deletePlan.id));
      invalidate(['plans']);
      setDeletePlan(null);
    } finally {
      setBusy(false);
    }
  };

  const openEditFeature = (f: Feature) => {
    setEditingFeature(f);
    setFeatureForm({ name: f.name, code: f.code, category: f.category ?? 'Training', isActive: f.isActive });
  };

  const handleSaveFeature = async () => {
    if (!editingFeature) return;
    setBusy(true);
    try {
      // Backend: PUT /admin/features/{id}. Code is immutable after creation on the backend side.
      await apiPut(BILLING.FEATURE(editingFeature.id), {
        name: featureForm.name,
        description: null,
        category: featureForm.category,
        isActive: featureForm.isActive,
      });
      invalidate(['features']);
      invalidate(['plan-features']);
      setEditingFeature(null);
    } finally {
      setBusy(false);
    }
  };

  const handleCreateFeature = async () => {
    if (!createFeatureForm.name.trim() || !createFeatureForm.code.trim()) return;
    setBusy(true);
    try {
      await apiPost(BILLING.FEATURES, {
        code: createFeatureForm.code,
        name: createFeatureForm.name,
        description: null,
        category: createFeatureForm.category,
      });
      invalidate(['features']);
      invalidate(['plan-features']);
      setCreateFeatureForm({ name: '', code: '', category: 'Training' });
      setShowFeatureModal(false);
    } finally {
      setBusy(false);
    }
  };

  const handleToggleMatrix = async (planId: string, featureId: string, currentlyEnabled: boolean) => {
    const cellKey = `${planId}:${featureId}`;
    setTogglingCell(cellKey);
    try {
      await togglePlanFeature.mutateAsync({ planId, featureId, isEnabled: !currentlyEnabled });
      setPendingChanges((n) => n + 1);
    } finally {
      setTogglingCell(null);
    }
  };

  return (
    <div className="space-y-6">
      {(plansError || featuresError) && (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">Failed to load plans data. Please refresh the page.</p>
        </div>
      )}
      <PageHeader
        title="Plans & Features"
        description="Manage subscription plans, features, and the feature matrix"
        actions={
          <div className="flex items-center gap-2">
            {pendingChanges > 0 && (
              <button
                onClick={() => setShowDeployModal(true)}
                className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 animate-pulse"
              >
                <Rocket className="h-4 w-4" />
                Deploy Changes
                <span className="bg-white/20 text-white text-xs px-1.5 py-0.5 rounded-full">{pendingChanges}</span>
              </button>
            )}
          </div>
        }
      />

      {/* Sticky Deploy Bar */}
      {pendingChanges > 0 && (
        <div className="sticky top-0 z-10 flex items-center gap-3 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 dark:border-emerald-800 dark:bg-emerald-950/20">
          <AlertCircle className="h-5 w-5 text-emerald-600 shrink-0" />
          <p className="text-sm font-medium text-emerald-800 dark:text-emerald-300">
            {pendingChanges} pending changes need to be deployed to take effect
          </p>
          <button
            onClick={() => setShowDeployModal(true)}
            className="ml-auto inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700"
          >
            <Rocket className="h-3.5 w-3.5" /> Deploy Now
          </button>
        </div>
      )}

      {/* Plans Management */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">Plans</h2>
          <button
            onClick={openCreatePlan}
            className="inline-flex items-center gap-2 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent"
          >
            <Plus className="h-4 w-4" /> Create Plan
          </button>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {subscriptionPlans.map((plan) => (
            <div key={plan.id} className="rounded-xl border bg-card p-5 shadow-sm">
              <div className="flex items-center justify-between mb-3">
                <StatusBadge variant={tierOf(plan)} className="text-sm px-3 py-1" />
                <div className="flex gap-1">
                  <button
                    onClick={() => openEditPlan(plan)}
                    aria-label={`Edit ${plan.name}`}
                    className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground"
                  >
                    <Edit className="h-3.5 w-3.5" />
                  </button>
                  <button
                    onClick={() => setDeletePlan(plan)}
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
          ))}
        </div>
      </div>

      {/* Features Management */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">Features ({features.length})</h2>
          <button
            onClick={() => setShowFeatureModal(true)}
            className="inline-flex items-center gap-2 rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-accent"
          >
            <Plus className="h-4 w-4" /> Add Feature
          </button>
        </div>
        <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Feature</th>
                  <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Code</th>
                  <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Category</th>
                  <th className="text-center py-2.5 px-4 font-semibold text-muted-foreground">Status</th>
                  <th className="py-2.5 px-4"></th>
                </tr>
              </thead>
              <tbody>
                {features.map((feature) => (
                  <tr key={feature.id} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="py-2 px-4 font-medium">{feature.name}</td>
                    <td className="py-2 px-4"><code className="text-xs bg-muted px-1.5 py-0.5 rounded">{feature.code}</code></td>
                    <td className="py-2 px-4 text-muted-foreground">{feature.category}</td>
                    <td className="py-2 px-4 text-center"><StatusBadge variant={feature.isActive ? 'active' : 'inactive'} /></td>
                    <td className="py-2 px-4 text-right">
                      <button
                        onClick={() => openEditFeature(feature)}
                        aria-label={`Edit ${feature.name}`}
                        className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground"
                      >
                        <Edit className="h-3.5 w-3.5" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Feature Matrix */}
      <div>
        <h2 className="text-lg font-semibold mb-4">Feature Matrix</h2>
        <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground sticky left-0 bg-muted/50 min-w-[200px]">Feature</th>
                  {subscriptionPlans.map((plan) => (
                    <th key={plan.id} className="text-center py-2.5 px-4 font-semibold text-muted-foreground min-w-[100px]">
                      <StatusBadge variant={tierOf(plan)} />
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {features.map((feature) => (
                  <tr key={feature.id} className="border-b last:border-0 hover:bg-muted/50">
                    <td className="py-2 px-4 font-medium sticky left-0 bg-card">{feature.name}</td>
                    {planIds.map((planId) => {
                      const enabled = featureMatrix[planId]?.[feature.id] ?? false;
                      const cellKey = `${planId}:${feature.id}`;
                      return (
                        <td key={planId} className="py-2 px-4 text-center">
                          <button
                            onClick={() => handleToggleMatrix(planId, feature.id, enabled)}
                            disabled={togglingCell === cellKey}
                            aria-label={`Toggle ${feature.name}`}
                            className="mx-auto disabled:opacity-50"
                          >
                            {enabled ? (
                              <div className="h-5 w-5 rounded bg-emerald-100 flex items-center justify-center mx-auto">
                                <svg className="h-3 w-3 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
                              </div>
                            ) : (
                              <div className="h-5 w-5 rounded bg-gray-100 flex items-center justify-center mx-auto dark:bg-gray-800">
                                <svg className="h-3 w-3 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                              </div>
                            )}
                          </button>
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Override Audit */}
      <div>
        <h2 className="text-lg font-semibold mb-4">Plan Change Audit Log</h2>
        <DataTable data={planChangeLogs} columns={overrideColumns} pageSize={5} />
      </div>

      {/* Deploy Modal */}
      <ConfirmModal
        open={showDeployModal}
        onClose={() => setShowDeployModal(false)}
        onConfirm={async () => {
          await deployMutation.mutateAsync({ scope: 'all' });
          setPendingChanges(0);
          setShowDeployModal(false);
        }}
        title="Deploy Configuration Changes"
        description="Deploy all pending configuration changes? This will push updates to all connected clients via SignalR and FCM."
        confirmLabel="Deploy Now"
        variant="success"
      />

      {/* Create/Edit Plan Modal */}
      <Modal
        open={showPlanModal}
        onClose={() => setShowPlanModal(false)}
        title={editingPlanId ? 'Edit Plan' : 'Create Plan'}
        size="lg"
        footer={
          <>
            <button onClick={() => setShowPlanModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={handleSavePlan}
              disabled={busy || !planForm.name?.trim() || (!editingPlanId && !planForm.code?.trim())}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busy ? 'Saving...' : editingPlanId ? 'Save Changes' : 'Create Plan'}
            </button>
          </>
        }
      >
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Plan Name</label>
            <input
              type="text"
              value={planForm.name}
              onChange={(e) => setPlanForm((p) => ({ ...p, name: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Plan Code</label>
            <input
              type="text"
              value={planForm.code}
              disabled={!!editingPlanId}
              placeholder="e.g. pro"
              onChange={(e) => setPlanForm((p) => ({ ...p, code: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm disabled:opacity-60"
            />
          </div>
          <div className="col-span-2">
            <label className="block text-sm font-medium mb-1.5">Description</label>
            <input
              type="text"
              value={planForm.description}
              onChange={(e) => setPlanForm((p) => ({ ...p, description: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Monthly Price (EGP)</label>
            <input
              type="number"
              value={planForm.monthlyPriceEgp}
              onChange={(e) => setPlanForm((p) => ({ ...p, monthlyPriceEgp: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Annual Price (EGP)</label>
            <input
              type="number"
              value={planForm.annualPriceEgp}
              onChange={(e) => setPlanForm((p) => ({ ...p, annualPriceEgp: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Member Limit</label>
            <input
              type="number"
              value={planForm.memberLimit}
              onChange={(e) => setPlanForm((p) => ({ ...p, memberLimit: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Trainer Limit</label>
            <input
              type="number"
              value={planForm.trainerLimit}
              onChange={(e) => setPlanForm((p) => ({ ...p, trainerLimit: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Staff Limit</label>
            <input
              type="number"
              value={planForm.staffLimit}
              onChange={(e) => setPlanForm((p) => ({ ...p, staffLimit: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Sort Order</label>
            <input
              type="number"
              value={planForm.sortOrder}
              onChange={(e) => setPlanForm((p) => ({ ...p, sortOrder: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div className="flex items-center gap-2 pt-6">
            <input
              id="plan-public"
              type="checkbox"
              checked={planForm.isPublic}
              onChange={(e) => setPlanForm((p) => ({ ...p, isPublic: e.target.checked }))}
              className="rounded border-input"
            />
            <label htmlFor="plan-public" className="text-sm">Visible to customers</label>
          </div>
          {editingPlanId && (
            <div className="flex items-center gap-2 pt-6">
              <input
                id="plan-active"
                type="checkbox"
                checked={planForm.isActive}
                onChange={(e) => setPlanForm((p) => ({ ...p, isActive: e.target.checked }))}
                className="rounded border-input"
              />
              <label htmlFor="plan-active" className="text-sm">Active</label>
            </div>
          )}
        </div>
      </Modal>

      {/* Delete Plan Confirm */}
      <ConfirmModal
        open={!!deletePlan}
        onClose={() => setDeletePlan(null)}
        onConfirm={handleDeletePlan}
        title="Delete Plan"
        description={`Delete the ${deletePlan?.name ?? ''} plan? Gyms currently on this plan must be migrated first.`}
        confirmLabel="Delete"
        variant="danger"
        isLoading={busy}
      />

      {/* Edit Feature Modal */}
      <Modal
        open={!!editingFeature}
        onClose={() => setEditingFeature(null)}
        title="Edit Feature"
        size="md"
        footer={
          <>
            <button onClick={() => setEditingFeature(null)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={handleSaveFeature}
              disabled={busy}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busy ? 'Saving...' : 'Save Changes'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Feature Name</label>
            <input
              type="text"
              value={featureForm.name}
              onChange={(e) => setFeatureForm((f) => ({ ...f, name: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Feature Code</label>
            <input
              type="text"
              value={featureForm.code}
              readOnly
              title="Feature code is immutable after creation"
              className="w-full h-9 rounded-lg border border-input bg-muted px-3 text-sm text-muted-foreground cursor-not-allowed"
            />
            <p className="text-xs text-muted-foreground mt-1">Code cannot be changed after creation.</p>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Category</label>
            <select
              value={featureForm.category}
              onChange={(e) => setFeatureForm((f) => ({ ...f, category: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              <option>Training</option><option>Nutrition</option><option>Progress</option>
              <option>Scheduling</option><option>Billing</option><option>Communication</option>
              <option>Gamification</option><option>Settings</option><option>Gyms</option>
              <option>Identity</option><option>Analytics</option>
            </select>
          </div>
          <div className="flex items-center gap-2">
            <input
              id="feature-active"
              type="checkbox"
              checked={featureForm.isActive}
              onChange={(e) => setFeatureForm((f) => ({ ...f, isActive: e.target.checked }))}
              className="rounded border-input"
            />
            <label htmlFor="feature-active" className="text-sm">Active</label>
          </div>
        </div>
      </Modal>

      {/* Add Feature Modal */}
      <Modal
        open={showFeatureModal}
        onClose={() => setShowFeatureModal(false)}
        title="Add Feature"
        size="md"
        footer={
          <>
            <button onClick={() => setShowFeatureModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={handleCreateFeature}
              disabled={busy || !createFeatureForm.name.trim() || !createFeatureForm.code.trim()}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busy ? 'Creating...' : 'Create Feature'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Feature Name</label>
            <input
              type="text"
              value={createFeatureForm.name}
              onChange={(e) => setCreateFeatureForm((f) => ({ ...f, name: e.target.value }))}
              placeholder="e.g. Remote Training"
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Feature Code</label>
            <input
              type="text"
              value={createFeatureForm.code}
              onChange={(e) => setCreateFeatureForm((f) => ({ ...f, code: e.target.value }))}
              placeholder="e.g. remote_training"
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Category</label>
            <select
              value={createFeatureForm.category}
              onChange={(e) => setCreateFeatureForm((f) => ({ ...f, category: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              <option>Training</option><option>Nutrition</option><option>Progress</option>
              <option>Scheduling</option><option>Billing</option><option>Communication</option>
              <option>Gamification</option><option>Settings</option><option>Gyms</option>
              <option>Identity</option><option>Analytics</option>
            </select>
          </div>
        </div>
      </Modal>
    </div>
  );
}

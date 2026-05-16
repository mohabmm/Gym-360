'use client';

import { useState } from 'react';
import {
  PageHeader,
  StatusBadge,
  Modal,
  formatCurrency,
  apiPost,
  apiPut,
  BILLING,
  useInvalidateQueries,
} from '@gym360/ui';
import { Edit, Plus, AlertCircle } from 'lucide-react';
import { usePlans, useFeatures, useGyms } from '@/lib/hooks';
import { CardSkeleton } from '@gym360/ui';
import type { SubscriptionPlan } from '@gym360/ui';

type PlanFormState = {
  code: string;
  name: string;
  description: string;
  monthlyPriceEgp: number;
  annualPriceEgp: number;
  memberLimit: number;
  trainerLimit: number;
  staffLimit: number;
  isPublic: boolean;
  isActive: boolean;
  sortOrder: number;
};

const EMPTY_PLAN_FORM: PlanFormState = {
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

function tierOf(plan: SubscriptionPlan): string {
  const c = plan.code?.toLowerCase() ?? '';
  if (c.includes('free')) return 'free';
  if (c.includes('business') || c.includes('enterprise')) return 'business';
  if (c.includes('pro')) return 'pro';
  return 'starter';
}

export default function SubscriptionsPage() {
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingPlan, setEditingPlan] = useState<SubscriptionPlan | null>(null);
  const [editForm, setEditForm] = useState<PlanFormState>(EMPTY_PLAN_FORM);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState<PlanFormState>(EMPTY_PLAN_FORM);
  const [busy, setBusy] = useState(false);
  const { invalidate } = useInvalidateQueries();

  const { data: plansData, isLoading: plansLoading, isError: plansError } = usePlans();
  const { data: gymsData } = useGyms();
  const { data: featuresData } = useFeatures();

  const subscriptionPlans = plansData?.data ?? [];
  const gyms = gymsData?.data ?? [];
  const features = featuresData?.data ?? [];

  const gymsByPlan = subscriptionPlans.map((plan) => ({
    ...plan,
    gymCount: gyms.filter((g) => g.planName === plan.name).length,
    totalRevenue: gyms.filter((g) => g.planName === plan.name).reduce((sum, g) => sum + g.monthlyRevenue, 0),
  }));

  const handleEditPlan = (plan: SubscriptionPlan) => {
    setEditingPlan(plan);
    setEditForm({
      code: plan.code ?? '',
      name: plan.name,
      description: plan.description ?? '',
      monthlyPriceEgp: plan.monthlyPriceEgp ?? 0,
      annualPriceEgp: plan.annualPriceEgp ?? 0,
      memberLimit: plan.memberLimit ?? 0,
      trainerLimit: plan.trainerLimit ?? 0,
      staffLimit: plan.staffLimit ?? 0,
      isPublic: plan.isPublic ?? true,
      isActive: plan.isActive ?? true,
      sortOrder: plan.sortOrder ?? 0,
    });
    setShowEditModal(true);
  };

  const openCreatePlan = () => {
    setCreateForm(EMPTY_PLAN_FORM);
    setShowCreateModal(true);
  };

  const handleCreatePlan = async () => {
    if (!createForm.name.trim() || !createForm.code.trim()) return;
    setBusy(true);
    try {
      await apiPost(BILLING.PLANS, {
        code: createForm.code,
        name: createForm.name,
        description: createForm.description || null,
        monthlyPrice: createForm.monthlyPriceEgp,
        annualPrice: createForm.annualPriceEgp,
        memberLimit: createForm.memberLimit,
        trainerLimit: createForm.trainerLimit,
        staffLimit: createForm.staffLimit,
        isPublic: createForm.isPublic,
        sortOrder: createForm.sortOrder,
      });
      invalidate(['plans']);
      invalidate(['plan-features']);
      setShowCreateModal(false);
    } finally {
      setBusy(false);
    }
  };

  const handleSaveEditedPlan = async () => {
    if (!editingPlan) return;
    setBusy(true);
    try {
      await apiPut(BILLING.PLAN(editingPlan.id), {
        name: editForm.name,
        description: editForm.description || null,
        monthlyPrice: editForm.monthlyPriceEgp,
        annualPrice: editForm.annualPriceEgp,
        memberLimit: editForm.memberLimit,
        trainerLimit: editForm.trainerLimit,
        staffLimit: editForm.staffLimit,
        isPublic: editForm.isPublic,
        isActive: editForm.isActive,
        sortOrder: editForm.sortOrder,
      });
      invalidate(['plans']);
      invalidate(['plan-features']);
      setShowEditModal(false);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-6">
      {plansError && (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">Failed to load subscription plans. Please refresh the page.</p>
        </div>
      )}
      <PageHeader
        title="Subscriptions"
        description="Manage subscription plans and billing"
        actions={
          <button
            onClick={openCreatePlan}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" />
            Create Plan
          </button>
        }
      />

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {plansLoading ? (
          Array.from({ length: 4 }).map((_, i) => <CardSkeleton key={i} />)
        ) : (gymsByPlan.map((plan) => (
          <div key={plan.id} className="rounded-xl border bg-card p-6 shadow-sm hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between mb-4">
              <StatusBadge variant={tierOf(plan)} className="text-sm px-3 py-1" />
              <button
                onClick={() => handleEditPlan(plan)}
                className="p-1.5 rounded-lg hover:bg-muted transition-colors text-muted-foreground"
              >
                <Edit className="h-4 w-4" />
              </button>
            </div>
            <h3 className="text-xl font-bold">{plan.name}</h3>
            <p className="text-2xl font-bold mt-2">
              {plan.monthlyPriceEgp === 0 ? 'Free' : formatCurrency(plan.monthlyPriceEgp)}
              {plan.monthlyPriceEgp > 0 && <span className="text-sm font-normal text-muted-foreground">/mo</span>}
            </p>
            <div className="mt-4 space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Gyms</span>
                <span className="font-medium">{plan.gymCount}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Revenue</span>
                <span className="font-medium">{formatCurrency(plan.totalRevenue)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Max Members</span>
                <span className="font-medium">{plan.memberLimit}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Max Trainers</span>
                <span className="font-medium">{plan.trainerLimit}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Max Staff</span>
                <span className="font-medium">{plan.staffLimit}</span>
              </div>
            </div>
          </div>
        )))}
      </div>

      <div className="rounded-xl border bg-card shadow-sm">
        <div className="p-4 border-b">
          <h3 className="text-sm font-semibold">Active Subscriptions</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Gym</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Plan</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Status</th>
                <th className="text-right py-2.5 px-4 font-semibold text-muted-foreground">Monthly</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Payment</th>
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

      <Modal
        open={showEditModal}
        onClose={() => setShowEditModal(false)}
        title={`Edit ${editingPlan?.name ?? ''} Plan`}
        size="lg"
        footer={
          <>
            <button onClick={() => setShowEditModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={handleSaveEditedPlan}
              disabled={busy}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busy ? 'Saving...' : 'Save Changes'}
            </button>
          </>
        }
      >
        {editingPlan && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1.5">Plan Name</label>
                <input
                  type="text"
                  value={editForm.name}
                  onChange={(e) => setEditForm((p) => ({ ...p, name: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Monthly Price (EGP)</label>
                <input
                  type="number"
                  value={editForm.monthlyPriceEgp}
                  onChange={(e) => setEditForm((p) => ({ ...p, monthlyPriceEgp: Number(e.target.value) }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Max Members</label>
                <input
                  type="number"
                  value={editForm.memberLimit}
                  onChange={(e) => setEditForm((p) => ({ ...p, memberLimit: Number(e.target.value) }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Max Trainers</label>
                <input
                  type="number"
                  value={editForm.trainerLimit}
                  onChange={(e) => setEditForm((p) => ({ ...p, trainerLimit: Number(e.target.value) }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Max Staff</label>
                <input
                  type="number"
                  value={editForm.staffLimit}
                  onChange={(e) => setEditForm((p) => ({ ...p, staffLimit: Number(e.target.value) }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Sort Order</label>
                <input
                  type="number"
                  value={editForm.sortOrder}
                  onChange={(e) => setEditForm((p) => ({ ...p, sortOrder: Number(e.target.value) }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
            </div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={editForm.isPublic}
                  onChange={(e) => setEditForm((p) => ({ ...p, isPublic: e.target.checked }))}
                  id="visible"
                  className="rounded border-input"
                />
                <label htmlFor="visible" className="text-sm">Visible to customers</label>
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={editForm.isActive}
                  onChange={(e) => setEditForm((p) => ({ ...p, isActive: e.target.checked }))}
                  id="active"
                  className="rounded border-input"
                />
                <label htmlFor="active" className="text-sm">Active</label>
              </div>
            </div>
            <div>
              <p className="text-xs text-muted-foreground border border-amber-200 bg-amber-50 rounded-lg px-3 py-2 dark:border-amber-800 dark:bg-amber-950/20">
                Feature assignments are managed via the <strong>Plans &amp; Features</strong> page using the Feature Matrix.
              </p>
              <div className="mt-2 grid grid-cols-2 gap-1 max-h-48 overflow-y-auto text-sm text-muted-foreground">
                {features.map((f) => <span key={f.id} className="truncate">{f.name}</span>)}
              </div>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        open={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        title="Create Plan"
        size="lg"
        footer={
          <>
            <button onClick={() => setShowCreateModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={handleCreatePlan}
              disabled={busy || !createForm.name.trim() || !createForm.code.trim()}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busy ? 'Creating...' : 'Create Plan'}
            </button>
          </>
        }
      >
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Plan Name</label>
            <input
              type="text"
              value={createForm.name}
              onChange={(e) => setCreateForm((p) => ({ ...p, name: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Plan Code</label>
            <input
              type="text"
              value={createForm.code}
              placeholder="e.g. pro"
              onChange={(e) => setCreateForm((p) => ({ ...p, code: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Monthly Price (EGP)</label>
            <input
              type="number"
              value={createForm.monthlyPriceEgp}
              onChange={(e) => setCreateForm((p) => ({ ...p, monthlyPriceEgp: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Annual Price (EGP)</label>
            <input
              type="number"
              value={createForm.annualPriceEgp}
              onChange={(e) => setCreateForm((p) => ({ ...p, annualPriceEgp: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Max Members</label>
            <input
              type="number"
              value={createForm.memberLimit}
              onChange={(e) => setCreateForm((p) => ({ ...p, memberLimit: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Max Trainers</label>
            <input
              type="number"
              value={createForm.trainerLimit}
              onChange={(e) => setCreateForm((p) => ({ ...p, trainerLimit: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Max Staff</label>
            <input
              type="number"
              value={createForm.staffLimit}
              onChange={(e) => setCreateForm((p) => ({ ...p, staffLimit: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Sort Order</label>
            <input
              type="number"
              value={createForm.sortOrder}
              onChange={(e) => setCreateForm((p) => ({ ...p, sortOrder: Number(e.target.value) }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div className="flex items-center gap-2 pt-6">
            <input
              id="create-visible"
              type="checkbox"
              checked={createForm.isPublic}
              onChange={(e) => setCreateForm((p) => ({ ...p, isPublic: e.target.checked }))}
              className="rounded border-input"
            />
            <label htmlFor="create-visible" className="text-sm">Visible to customers</label>
          </div>
        </div>
      </Modal>
    </div>
  );
}

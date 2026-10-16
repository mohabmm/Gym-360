'use client';

import { useState } from 'react';
import { PageHeader, apiPost, apiPut, BILLING, useInvalidateQueries } from '@gym360/ui';
import { Plus } from 'lucide-react';
import { usePlans, useFeatures, useGyms } from '@/lib/hooks';
import type { SubscriptionPlan } from '@gym360/ui';
import {
  EMPTY_PLAN_FORM,
  hasNameAndCode,
  planToForm,
  toCreatePlanBody,
  toUpdatePlanBody,
  withBusy,
  type PlanFormState,
} from '@/lib/plans';
import { LoadErrorBanner } from '@/components/plans/load-error-banner';
import { ActiveSubscriptionsTable, PlanCardsGrid, withGymStats } from '@/components/subscriptions/subscription-sections';
import { CreateSubscriptionPlanModal, EditSubscriptionPlanModal } from '@/components/subscriptions/subscription-modals';

function useSubscriptionPlanEditor() {
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingPlan, setEditingPlan] = useState<SubscriptionPlan | null>(null);
  const [editForm, setEditForm] = useState<PlanFormState>(EMPTY_PLAN_FORM);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState<PlanFormState>(EMPTY_PLAN_FORM);
  const [busy, setBusy] = useState(false);
  const { invalidate } = useInvalidateQueries();

  const invalidatePlans = () => {
    invalidate(['plans']);
    invalidate(['plan-features']);
  };

  const handleEditPlan = (plan: SubscriptionPlan) => {
    setEditingPlan(plan);
    setEditForm(planToForm(plan));
    setShowEditModal(true);
  };

  const openCreatePlan = () => {
    setCreateForm(EMPTY_PLAN_FORM);
    setShowCreateModal(true);
  };

  const canCreate = hasNameAndCode(createForm);

  const handleCreatePlan = async () => {
    if (!canCreate) return;
    await withBusy(setBusy, async () => {
      await apiPost(BILLING.PLANS, toCreatePlanBody(createForm));
      invalidatePlans();
      setShowCreateModal(false);
    });
  };

  const handleSaveEditedPlan = async () => {
    if (!editingPlan) return;
    await withBusy(setBusy, async () => {
      await apiPut(BILLING.PLAN(editingPlan.id), toUpdatePlanBody(editForm));
      invalidatePlans();
      setShowEditModal(false);
    });
  };

  return {
    busy,
    canCreate,
    editingPlan,
    showEditModal,
    closeEditModal: () => setShowEditModal(false),
    editForm,
    setEditForm,
    showCreateModal,
    closeCreateModal: () => setShowCreateModal(false),
    createForm,
    setCreateForm,
    handleEditPlan,
    openCreatePlan,
    handleCreatePlan,
    handleSaveEditedPlan,
  };
}

export default function SubscriptionsPage() {
  const editor = useSubscriptionPlanEditor();
  const { data: plansData, isLoading: plansLoading, isError: plansError } = usePlans();
  const { data: gymsData } = useGyms();
  const { data: featuresData } = useFeatures();

  const gyms = gymsData?.data ?? [];
  const gymsByPlan = withGymStats(plansData?.data ?? [], gyms);

  return (
    <div className="space-y-6">
      {plansError && <LoadErrorBanner message="Failed to load subscription plans. Please refresh the page." />}
      <PageHeader
        title="Subscriptions"
        description="Manage subscription plans and billing"
        actions={
          <button
            onClick={editor.openCreatePlan}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" />
            Create Plan
          </button>
        }
      />

      <PlanCardsGrid isLoading={plansLoading} plans={gymsByPlan} onEdit={editor.handleEditPlan} />

      <ActiveSubscriptionsTable gyms={gyms} />

      <EditSubscriptionPlanModal
        open={editor.showEditModal}
        onClose={editor.closeEditModal}
        busy={editor.busy}
        form={editor.editForm}
        setForm={editor.setEditForm}
        planName={editor.editingPlan?.name}
        onSave={editor.handleSaveEditedPlan}
        features={featuresData?.data ?? []}
      />

      <CreateSubscriptionPlanModal
        open={editor.showCreateModal}
        onClose={editor.closeCreateModal}
        busy={editor.busy}
        form={editor.createForm}
        setForm={editor.setCreateForm}
        canCreate={editor.canCreate}
        onCreate={editor.handleCreatePlan}
      />
    </div>
  );
}

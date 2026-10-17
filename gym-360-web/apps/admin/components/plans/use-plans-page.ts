'use client';

import { useState } from 'react';
import { apiPost, apiPut, apiDelete, BILLING, useInvalidateQueries } from '@gym360/ui';
import type { Feature, SubscriptionPlan } from '@gym360/ui';
import { useDeploy, useTogglePlanFeature } from '@/lib/hooks';
import {
  EMPTY_PLAN_FORM,
  hasNameAndCode,
  matrixCellKey,
  planToForm,
  toCreatePlanBody,
  toUpdatePlanBody,
  withBusy,
  type PlanFormState,
} from '@/lib/plans';
import type { CreateFeatureForm, FeatureForm } from './plan-modals';

const EMPTY_CREATE_FEATURE_FORM: CreateFeatureForm = { name: '', code: '', category: 'Training' };

/** Create / edit / delete plan state + handlers. `setBusy` is shared with the feature editor. */
export function usePlanEditor(setBusy: (busy: boolean) => void) {
  const { invalidate } = useInvalidateQueries();
  const [showPlanModal, setShowPlanModal] = useState(false);
  const [planForm, setPlanForm] = useState<PlanFormState>(EMPTY_PLAN_FORM);
  const [editingPlanId, setEditingPlanId] = useState<string | null>(null);
  const [deletePlan, setDeletePlan] = useState<SubscriptionPlan | null>(null);

  const openCreatePlan = () => {
    setEditingPlanId(null);
    setPlanForm(EMPTY_PLAN_FORM);
    setShowPlanModal(true);
  };

  const openEditPlan = (p: SubscriptionPlan) => {
    setEditingPlanId(p.id);
    setPlanForm(planToForm(p));
    setShowPlanModal(true);
  };

  const persistPlan = () =>
    editingPlanId
      ? apiPut(BILLING.PLAN(editingPlanId), toUpdatePlanBody(planForm))
      : apiPost(BILLING.PLANS, toCreatePlanBody(planForm));

  const handleSavePlan = async () => {
    if (!hasNameAndCode(planForm)) return;
    await withBusy(setBusy, async () => {
      await persistPlan();
      invalidate(['plans']);
      invalidate(['plan-features']);
      setShowPlanModal(false);
    });
  };

  const handleDeletePlan = async () => {
    if (!deletePlan) return;
    await withBusy(setBusy, async () => {
      await apiDelete(BILLING.PLAN(deletePlan.id));
      invalidate(['plans']);
      setDeletePlan(null);
    });
  };

  return {
    showPlanModal,
    closePlanModal: () => setShowPlanModal(false),
    planForm,
    setPlanForm,
    isEditingPlan: !!editingPlanId,
    deletePlan,
    setDeletePlan,
    openCreatePlan,
    openEditPlan,
    handleSavePlan,
    handleDeletePlan,
  };
}

/** Edit-feature and add-feature modal state + handlers. */
export function useFeatureEditor(setBusy: (busy: boolean) => void) {
  const { invalidate } = useInvalidateQueries();
  const [showFeatureModal, setShowFeatureModal] = useState(false);
  const [editingFeature, setEditingFeature] = useState<Feature | null>(null);
  const [featureForm, setFeatureForm] = useState<FeatureForm>({ name: '', code: '', category: 'Training', isActive: true });
  const [createFeatureForm, setCreateFeatureForm] = useState<CreateFeatureForm>(EMPTY_CREATE_FEATURE_FORM);

  const invalidateFeatures = () => {
    invalidate(['features']);
    invalidate(['plan-features']);
  };

  const openEditFeature = (f: Feature) => {
    setEditingFeature(f);
    setFeatureForm({ name: f.name, code: f.code, category: f.category ?? 'Training', isActive: f.isActive });
  };

  const handleSaveFeature = async () => {
    if (!editingFeature) return;
    await withBusy(setBusy, async () => {
      // Backend: PUT /admin/features/{id}. Code is immutable after creation on the backend side.
      await apiPut(BILLING.FEATURE(editingFeature.id), {
        name: featureForm.name,
        description: null,
        category: featureForm.category,
        isActive: featureForm.isActive,
      });
      invalidateFeatures();
      setEditingFeature(null);
    });
  };

  const canCreateFeature = hasNameAndCode(createFeatureForm);

  const handleCreateFeature = async () => {
    if (!canCreateFeature) return;
    await withBusy(setBusy, async () => {
      await apiPost(BILLING.FEATURES, {
        code: createFeatureForm.code,
        name: createFeatureForm.name,
        description: null,
        category: createFeatureForm.category,
      });
      invalidateFeatures();
      setCreateFeatureForm(EMPTY_CREATE_FEATURE_FORM);
      setShowFeatureModal(false);
    });
  };

  return {
    showFeatureModal,
    openCreateFeature: () => setShowFeatureModal(true),
    closeCreateFeature: () => setShowFeatureModal(false),
    editingFeature,
    closeEditFeature: () => setEditingFeature(null),
    featureForm,
    setFeatureForm,
    createFeatureForm,
    setCreateFeatureForm,
    canCreateFeature,
    openEditFeature,
    handleSaveFeature,
    handleCreateFeature,
  };
}

/** Matrix cell toggles accumulate as pending changes until the admin deploys them. */
export function useMatrixDeploy() {
  const togglePlanFeature = useTogglePlanFeature();
  const deployMutation = useDeploy();
  const [pendingChanges, setPendingChanges] = useState(0);
  const [showDeployModal, setShowDeployModal] = useState(false);
  const [togglingCell, setTogglingCell] = useState<string | null>(null);

  const handleToggleMatrix = async (planId: string, featureId: string, currentlyEnabled: boolean) => {
    setTogglingCell(matrixCellKey(planId, featureId));
    try {
      await togglePlanFeature.mutateAsync({ planId, featureId, isEnabled: !currentlyEnabled });
      setPendingChanges((n) => n + 1);
    } finally {
      setTogglingCell(null);
    }
  };

  const handleDeploy = async () => {
    await deployMutation.mutateAsync({ scope: 'all' });
    setPendingChanges(0);
    setShowDeployModal(false);
  };

  return {
    pendingChanges,
    togglingCell,
    showDeployModal,
    openDeployModal: () => setShowDeployModal(true),
    closeDeployModal: () => setShowDeployModal(false),
    handleToggleMatrix,
    handleDeploy,
  };
}

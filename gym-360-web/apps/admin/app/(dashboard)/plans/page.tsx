'use client';

import { useState } from 'react';
import { PageHeader, ConfirmModal } from '@gym360/ui';
import { usePlans, useFeatures, usePlanFeatureMatrix, usePlanChangeLogs } from '@/lib/hooks';
import { buildFeatureMatrix } from '@/lib/plans';
import { LoadErrorBanner } from '@/components/plans/load-error-banner';
import {
  DeployBar,
  DeployHeaderButton,
  FeatureMatrixSection,
  FeaturesSection,
  PlanChangeLogSection,
  PlansSection,
} from '@/components/plans/plans-sections';
import { CreateFeatureModal, EditFeatureModal, PlanFormModal } from '@/components/plans/plan-modals';
import { useFeatureEditor, useMatrixDeploy, usePlanEditor } from '@/components/plans/use-plans-page';

function usePlansData() {
  const { data: plansData, isError: plansError } = usePlans();
  const { data: featuresData, isError: featuresError } = useFeatures();
  const { data: matrixData } = usePlanFeatureMatrix();

  // Backend GetFeatureMatrix returns { plans, features, matrix }. Prefer that shape when it's
  // available so we don't need two separate calls; fall back to the standalone lists otherwise.
  const matrix = matrixData?.data;
  return {
    hasError: plansError || featuresError,
    subscriptionPlans: matrix?.plans ?? plansData?.data ?? [],
    features: matrix?.features ?? featuresData?.data ?? [],
    featureMatrix: buildFeatureMatrix(matrix?.matrix ?? []),
  };
}

function usePlanChangeLogItems() {
  const { data: changeLogsData } = usePlanChangeLogs();
  return changeLogsData?.data?.items ?? [];
}

export default function PlansPage() {
  const [busy, setBusy] = useState(false);
  const { hasError, subscriptionPlans, features, featureMatrix } = usePlansData();
  const planChangeLogs = usePlanChangeLogItems();
  const deploy = useMatrixDeploy();
  const planEditor = usePlanEditor(setBusy);
  const featureEditor = useFeatureEditor(setBusy);
  const { deletePlan } = planEditor;

  return (
    <div className="space-y-6">
      {hasError && <LoadErrorBanner message="Failed to load plans data. Please refresh the page." />}
      <PageHeader
        title="Plans & Features"
        description="Manage subscription plans, features, and the feature matrix"
        actions={<DeployHeaderButton pendingChanges={deploy.pendingChanges} onDeploy={deploy.openDeployModal} />}
      />

      <DeployBar pendingChanges={deploy.pendingChanges} onDeploy={deploy.openDeployModal} />

      <PlansSection
        plans={subscriptionPlans}
        onCreate={planEditor.openCreatePlan}
        onEdit={planEditor.openEditPlan}
        onDelete={planEditor.setDeletePlan}
      />

      <FeaturesSection features={features} onAdd={featureEditor.openCreateFeature} onEdit={featureEditor.openEditFeature} />

      <FeatureMatrixSection
        plans={subscriptionPlans}
        features={features}
        featureMatrix={featureMatrix}
        togglingCell={deploy.togglingCell}
        onToggle={deploy.handleToggleMatrix}
      />

      {/* Override Audit */}
      <PlanChangeLogSection logs={planChangeLogs} />

      {/* Deploy Modal */}
      <ConfirmModal
        open={deploy.showDeployModal}
        onClose={deploy.closeDeployModal}
        onConfirm={deploy.handleDeploy}
        title="Deploy Configuration Changes"
        description="Deploy all pending configuration changes? This will push updates to all connected clients via SignalR and FCM."
        confirmLabel="Deploy Now"
        variant="success"
      />

      <PlanFormModal
        open={planEditor.showPlanModal}
        onClose={planEditor.closePlanModal}
        onSave={planEditor.handleSavePlan}
        busy={busy}
        isEditing={planEditor.isEditingPlan}
        form={planEditor.planForm}
        setForm={planEditor.setPlanForm}
      />

      {/* Delete Plan Confirm */}
      <ConfirmModal
        open={!!deletePlan}
        onClose={() => planEditor.setDeletePlan(null)}
        onConfirm={planEditor.handleDeletePlan}
        title="Delete Plan"
        description={`Delete the ${deletePlan?.name ?? ''} plan? Gyms currently on this plan must be migrated first.`}
        confirmLabel="Delete"
        variant="danger"
        isLoading={busy}
      />

      <EditFeatureModal
        open={!!featureEditor.editingFeature}
        onClose={featureEditor.closeEditFeature}
        onSave={featureEditor.handleSaveFeature}
        busy={busy}
        form={featureEditor.featureForm}
        setForm={featureEditor.setFeatureForm}
      />

      <CreateFeatureModal
        open={featureEditor.showFeatureModal}
        onClose={featureEditor.closeCreateFeature}
        onCreate={featureEditor.handleCreateFeature}
        busy={busy}
        canCreate={featureEditor.canCreateFeature}
        form={featureEditor.createFeatureForm}
        setForm={featureEditor.setCreateFeatureForm}
      />
    </div>
  );
}

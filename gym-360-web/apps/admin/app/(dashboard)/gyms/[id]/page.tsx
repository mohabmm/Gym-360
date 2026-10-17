'use client';

import { useState, type ReactNode } from 'react';
import { useParams } from 'next/navigation';
import { PageHeader, PageSkeleton, type Gym, type SubscriptionPlan } from '@gym360/ui';
import { useGym, usePlans, useFeatures, usePlanFeatureMatrix, useGymFeatureOverrides } from '@/lib/hooks';
import { GymSettingsPanel } from '@/components/gym-settings-panel';
import { GymRevenuePanel } from '@/components/gym-revenue-panel';
import { buildFeatureMatrix, summarizeGymPlan, type GymDetailModal, type GymDetailTab, type GymPlanSummary } from '@/lib/gym-detail';
import { ActionErrorBanner, GymHeaderActions, GymStatusBadges, GymTabBar } from '@/components/gym-detail/gym-detail-header';
import { GymOverviewTab } from '@/components/gym-detail/gym-overview-tab';
import { GymMembersTab, GymStaffTab, GymTrainersTab } from '@/components/gym-detail/gym-people-tabs';
import { GymSubscriptionTab } from '@/components/gym-detail/gym-subscription-tab';
import {
  ChangePlanModal,
  DeleteGymModal,
  EditGymModal,
  ReactivateGymModal,
  SuspendGymModal,
} from '@/components/gym-detail/gym-detail-modals';
import { AddStaffModal } from '@/components/gym-detail/add-staff-modal';
import { useGymDetailActions, type GymDetailActions } from '@/components/gym-detail/use-gym-detail-actions';

/** Plans + features + per-gym overrides backing the Subscription & Features tab and Change Plan modal. */
function useGymCatalog(gymId: string) {
  const { data: plansData } = usePlans();
  const { data: featuresData } = useFeatures();
  const { data: matrixData } = usePlanFeatureMatrix();
  const { data: overridesData } = useGymFeatureOverrides(gymId);
  const matrix = matrixData?.data;
  return {
    subscriptionPlans: matrix?.plans ?? plansData?.data ?? [],
    features: matrix?.features ?? featuresData?.data ?? [],
    featureMatrix: buildFeatureMatrix(matrix?.matrix ?? []),
    gymOverrides: overridesData?.data ?? [],
  };
}

type GymCatalog = ReturnType<typeof useGymCatalog>;

interface GymTabContentProps {
  activeTab: GymDetailTab;
  gymId: string;
  gym: Gym;
  summary: GymPlanSummary;
  catalog: GymCatalog;
  openModal: (modal: GymDetailModal) => void;
}

function GymTabContent({ activeTab, gymId, gym, summary, catalog, openModal }: GymTabContentProps) {
  const renderers: Record<GymDetailTab, () => ReactNode> = {
    Overview: () => <GymOverviewTab gym={gym} summary={summary} />,
    Members: () => <GymMembersTab gymId={gymId} count={gym.activeMemberCount} />,
    Trainers: () => <GymTrainersTab gymId={gymId} count={gym.activeTrainerCount} />,
    Staff: () => <GymStaffTab gymId={gymId} onAddStaff={() => openModal('staff')} />,
    Revenue: () => <GymRevenuePanel gymId={gymId} />,
    'Subscription & Features': () => (
      <GymSubscriptionTab
        gymId={gymId}
        gym={gym}
        summary={summary}
        features={catalog.features}
        featureMatrix={catalog.featureMatrix}
        gymOverrides={catalog.gymOverrides}
        onChangePlan={() => openModal('changePlan')}
      />
    ),
    Settings: () => <GymSettingsPanel gymId={gymId} gymName={gym.name} />,
  };
  return <>{renderers[activeTab]()}</>;
}

interface GymDetailModalsProps {
  openModal: GymDetailModal | null;
  gymId: string;
  gym: Gym;
  actions: GymDetailActions;
  plans: SubscriptionPlan[];
  currentPlanId: string;
  onClose: () => void;
}

/** At most one modal is mounted at a time so each starts with fresh form state. */
function GymDetailModals({ openModal, gymId, gym, actions, plans, currentPlanId, onClose }: GymDetailModalsProps) {
  const modalProps = { gym, actions, onClose };
  const renderers: Record<Exclude<GymDetailModal, 'staff'>, () => ReactNode> = {
    suspend: () => <SuspendGymModal {...modalProps} />,
    reactivate: () => <ReactivateGymModal {...modalProps} />,
    delete: () => <DeleteGymModal {...modalProps} />,
    edit: () => <EditGymModal {...modalProps} />,
    changePlan: () => <ChangePlanModal actions={actions} plans={plans} currentPlanId={currentPlanId} onClose={onClose} />,
  };
  const active = openModal && openModal !== 'staff' ? renderers[openModal]() : null;
  return (
    <>
      {active}
      <AddStaffModal gymId={gymId} gymName={gym.name} open={openModal === 'staff'} onClose={onClose} />
    </>
  );
}

function gymDescription(gym: Gym, ownerName: string): string {
  return `${gym.address?.city ?? gym.gymCode} | Code: ${gym.gymCode} | Owner: ${ownerName}`;
}

export default function GymDetailPage() {
  const params = useParams();
  const gymId = params.id as string;
  const [activeTab, setActiveTab] = useState<GymDetailTab>('Overview');
  const [openModal, setOpenModal] = useState<GymDetailModal | null>(null);
  const actions = useGymDetailActions(gymId);
  const { data: gymData, isLoading } = useGym(gymId);
  const catalog = useGymCatalog(gymId);

  const gym = gymData?.data;
  if (isLoading) return <PageSkeleton />;
  if (!gym) return <div className="p-6 text-muted-foreground">Gym not found</div>;

  const summary = summarizeGymPlan(gym, catalog.subscriptionPlans);

  return (
    <div className="space-y-6">
      <ActionErrorBanner message={actions.actionError} onDismiss={actions.clearActionError} />
      <PageHeader
        title={gym.name}
        description={gymDescription(gym, summary.ownerName)}
        breadcrumbs={[{ label: 'Gyms', href: '/gyms' }, { label: gym.name }]}
        actions={
          <GymHeaderActions
            status={gym.status}
            onEdit={() => setOpenModal('edit')}
            onSuspend={() => setOpenModal('suspend')}
            onReactivate={() => setOpenModal('reactivate')}
            onDelete={() => setOpenModal('delete')}
          />
        }
      />

      {/* Status badges row */}
      <GymStatusBadges gym={gym} summary={summary} />

      {/* Tabs */}
      <GymTabBar activeTab={activeTab} onChange={setActiveTab} />

      {/* Tab Content */}
      <GymTabContent
        activeTab={activeTab}
        gymId={gymId}
        gym={gym}
        summary={summary}
        catalog={catalog}
        openModal={setOpenModal}
      />

      {/* Modals */}
      <GymDetailModals
        openModal={openModal}
        gymId={gymId}
        gym={gym}
        actions={actions}
        plans={catalog.subscriptionPlans}
        currentPlanId={summary.plan?.id ?? ''}
        onClose={() => setOpenModal(null)}
      />
    </div>
  );
}

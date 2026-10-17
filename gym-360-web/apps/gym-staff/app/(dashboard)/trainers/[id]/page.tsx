'use client';

import { useState } from 'react';
import { useParams } from 'next/navigation';
import { useSession } from 'next-auth/react';
import { toast } from 'sonner';
import { PageHeader } from '@gym360/ui';
import { Edit } from 'lucide-react';
import Link from 'next/link';
import {
  useTrainer,
  useTrainers,
  useTrainerClients,
  useTrainerSessions,
  useUpdateTrainerProfile,
} from '@/lib/hooks/useTrainers';
import { useTrainerRate, useTrainerEarnings, useSetTrainerRate } from '@/lib/hooks/useTrainerEarnings';
import { EditProfileModalForm, EditRateModal } from '@/components/trainers/TrainerModals';
import { TrainerHeaderCard, TrainerKpis } from '@/components/trainers/TrainerHeaderCard';
import {
  TrainerTabBar,
  TrainerProfileTab,
  TrainerMembersTab,
  TrainerSessionsTab,
  TrainerRatingsTab,
  type TrainerTab,
} from '@/components/trainers/TrainerDetailTabs';
import { TrainerEarningsTab } from '@/components/trainers/TrainerEarningsTab';

// `t1`, `t2`, … are positional aliases into the trainer list; anything else is a real id.
function useResolvedTrainer(trainerId: string) {
  const isAlias = /^t\d+$/i.test(trainerId);
  const aliasIndex = isAlias ? Math.max(0, parseInt(trainerId.slice(1), 10) - 1) : -1;
  const { data: trainersList } = useTrainers();
  const { data: trainerDirect, isLoading: trainerLoading } = useTrainer(trainerId);
  if (isAlias) {
    return { trainer: trainersList?.[aliasIndex] ?? trainersList?.[0], isLoading: !trainersList };
  }
  return { trainer: trainerDirect, isLoading: trainerLoading };
}

// Tab-scoped queries only fire while their tab is open (empty id disables them).
const idWhenTab = (active: TrainerTab, tab: TrainerTab, id: string) => (active === tab ? id : '');

const profileErrorMessage = (e: unknown) =>
  (e as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message;

function TrainerNotFound() {
  return (
    <div className="text-center py-20">
      <h2 className="text-lg font-semibold">Trainer not found</h2>
      <Link href="/trainers" className="text-primary text-sm mt-2 inline-block hover:underline">
        Back to Trainers
      </Link>
    </div>
  );
}

export default function TrainerDetailPage() {
  const params = useParams();
  const trainerId = params.id as string;
  const { data: session } = useSession();
  const gymId = session?.user?.gymId ?? '';

  const { trainer, isLoading } = useResolvedTrainer(trainerId);

  const [activeTab, setActiveTab] = useState<TrainerTab>('Profile');
  const [editRateOpen, setEditRateOpen] = useState(false);
  const [editProfileOpen, setEditProfileOpen] = useState(false);

  const { data: rateData, isLoading: rateLoading } = useTrainerRate(gymId, trainerId);
  const { data: earningsData, isLoading: earningsLoading } = useTrainerEarnings(gymId, trainerId, activeTab === 'Earnings');

  const resolvedTrainerId = trainer?.id ?? trainerId;
  const { data: clientsData, isLoading: clientsLoading } = useTrainerClients(
    idWhenTab(activeTab, 'Members', resolvedTrainerId),
  );
  const { data: sessionsData, isLoading: sessionsLoading } = useTrainerSessions(
    idWhenTab(activeTab, 'Sessions', resolvedTrainerId),
  );
  const updateProfileMutation = useUpdateTrainerProfile(resolvedTrainerId);
  const setRateMutation = useSetTrainerRate(gymId, trainerId, () => setEditRateOpen(false));

  if (isLoading) {
    return (
      <div className="text-center py-20">
        <p className="text-sm text-muted-foreground">Loading trainer…</p>
      </div>
    );
  }

  if (!trainer) return <TrainerNotFound />;

  const currentRate = rateData?.ratePerSession ?? null;

  const saveProfile = async (payload: { bio: string; specializations: string[] }) => {
    try {
      await updateProfileMutation.mutateAsync(payload);
      toast.success('Profile saved.');
      setEditProfileOpen(false);
    } catch (e) {
      toast.error(profileErrorMessage(e) ?? 'Failed to save profile.');
    }
  };

  const tabContent: Record<TrainerTab, () => React.ReactNode> = {
    Profile: () => <TrainerProfileTab trainer={trainer} />,
    Members: () => <TrainerMembersTab trainer={trainer} clients={clientsData} loading={clientsLoading} />,
    Sessions: () => <TrainerSessionsTab sessions={sessionsData} loading={sessionsLoading} />,
    Ratings: () => <TrainerRatingsTab />,
    Earnings: () => (
      <TrainerEarningsTab
        rate={rateData}
        loading={rateLoading}
        onEditRate={() => setEditRateOpen(true)}
        earnings={earningsData}
        earningsLoading={earningsLoading}
      />
    ),
  };

  return (
    <div className="space-y-6">
      {editRateOpen && (
        <EditRateModal
          currentRate={currentRate}
          onClose={() => setEditRateOpen(false)}
          onSave={(rate) => setRateMutation.mutate(rate)}
          isSaving={setRateMutation.isPending}
        />
      )}

      <PageHeader
        title={trainer.name}
        description={trainer.nameAr}
        breadcrumbs={[
          { label: 'Trainers', href: '/trainers' },
          { label: trainer.name },
        ]}
        actions={
          <button
            onClick={() => setEditProfileOpen(true)}
            className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" /> Edit Profile
          </button>
        }
      />

      {/* Header Card */}
      <TrainerHeaderCard trainer={trainer} />

      {/* KPIs */}
      <TrainerKpis trainer={trainer} rateLoading={rateLoading} currentRate={currentRate} />

      {/* Tabs */}
      <TrainerTabBar active={activeTab} onChange={setActiveTab} />

      {/* Tab Content */}
      {tabContent[activeTab]()}

      <EditProfileModalForm
        open={editProfileOpen}
        onClose={() => setEditProfileOpen(false)}
        initialBio={trainer.bio ?? ''}
        initialSpecializations={trainer.specializations ?? []}
        onSave={saveProfile}
        isSaving={updateProfileMutation.isPending}
      />
    </div>
  );
}

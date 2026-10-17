'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { useSession } from 'next-auth/react';
import type { Session } from 'next-auth';
import { PageHeader, StatusBadge } from '@gym360/ui';
import { Edit, Trash2, UserCheck } from 'lucide-react';
import { useMember, useUpdateMember, useDeleteMember, useMemberPackages } from '@/lib/hooks/useMembers';
import { useTrainers } from '@/lib/hooks/useTrainers';
import { usePayments } from '@/lib/hooks/useBilling';
import { MemberTabBar, MemberTabContent, type MemberTab } from '@/components/members/MemberDetailTabs';
import { AssignTrainerModal } from '@/components/members/AssignTrainerModal';
import { EditMemberModal, RemoveMemberModal, type EditMemberForm } from '@/components/members/MemberModals';

// BE requires gym_owner_or_admin to DELETE members; gate the UI on the same
// claim so staff (manager/receptionist) don't see a button that would 403.
function isOwnerOrAdmin(session: Session | null) {
  const principalType = session?.user?.principalType;
  const adminRole = (session?.user as unknown as { adminRole?: string } | undefined)?.adminRole;
  return principalType === 'Owner' || principalType === 'PlatformAdmin' || !!adminRole;
}

const errorText = (e: unknown, fallback: string) => (e instanceof Error ? e.message : fallback);

function MemberNotFound() {
  return (
    <div className="space-y-6">
      <PageHeader title="Member Not Found" breadcrumbs={[{ label: 'Members', href: '/members' }, { label: 'Not Found' }]} />
      <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
        <p className="text-muted-foreground">No member found with this ID.</p>
      </div>
    </div>
  );
}

interface HeaderActionsProps {
  canRemove: boolean;
  onAssign: () => void;
  onEdit: () => void;
  onRemove: () => void;
}

function MemberHeaderActions({ canRemove, onAssign, onEdit, onRemove }: HeaderActionsProps) {
  return (
    <div className="flex items-center gap-2">
      <button
        onClick={onAssign}
        className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-700"
      >
        <UserCheck className="h-4 w-4" /> Assign Trainer
      </button>
      <button
        onClick={onEdit}
        className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium hover:bg-accent"
      >
        <Edit className="h-4 w-4" /> Edit
      </button>
      {canRemove && (
        <button
          onClick={onRemove}
          className="inline-flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100"
        >
          <Trash2 className="h-4 w-4" /> Remove
        </button>
      )}
    </div>
  );
}

export default function MemberDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { data: session } = useSession();
  const canRemove = isOwnerOrAdmin(session);
  const memberId = params.id as string;
  const { data: member, isLoading: memberLoading } = useMember(memberId);
  const { data: payments = [] } = usePayments();
  const updateMember = useUpdateMember();
  const deleteMember = useDeleteMember();
  const { data: trainers = [] } = useTrainers();
  const { data: memberPackages = [], isLoading: packagesLoading } = useMemberPackages(memberId);
  const trainerNameById = new Map(trainers.map((t) => [t.id, t.name] as const));
  const [activeTab, setActiveTab] = useState<MemberTab>('Profile');
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [editForm, setEditForm] = useState<EditMemberForm>({ name: '', nameAr: '', email: '', phone: '' });

  const handleConfirmDelete = () => {
    deleteMember.mutate(memberId, {
      onSuccess: () => {
        toast.success('Member removed.');
        setShowDeleteModal(false);
        router.push('/members');
      },
      onError: (e: unknown) => toast.error(errorText(e, 'Failed to remove member.')),
    });
  };

  const handleSaveEdit = () => {
    updateMember.mutate(
      { id: memberId, ...editForm },
      {
        onSuccess: () => {
          toast.success('Member updated.');
          setShowEditModal(false);
        },
        onError: () => {
          toast.error('Failed to update member.');
          setShowEditModal(false);
        },
      },
    );
  };

  if (memberLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <p className="text-muted-foreground text-sm">Loading member…</p>
      </div>
    );
  }

  if (!member) return <MemberNotFound />;

  const openEdit = () => {
    setEditForm({ name: member.name, nameAr: member.nameAr, email: member.email, phone: member.phone });
    setShowEditModal(true);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={member.name}
        description={member.nameAr}
        breadcrumbs={[{ label: 'Members', href: '/members' }, { label: member.name }]}
        actions={
          <MemberHeaderActions
            canRemove={canRemove}
            onAssign={() => setShowAssignModal(true)}
            onEdit={openEdit}
            onRemove={() => setShowDeleteModal(true)}
          />
        }
      />

      <div className="flex items-center gap-3">
        <StatusBadge variant={member.status} dot />
        <span className="text-sm text-muted-foreground capitalize">{(member.membershipType ?? '').replace(/_/g, ' ')}</span>
        <span className="text-sm text-muted-foreground">{member.section}</span>
      </div>

      <MemberTabBar active={activeTab} onChange={setActiveTab} />

      <MemberTabContent
        tab={activeTab}
        member={member}
        payments={payments}
        packages={memberPackages}
        loading={packagesLoading}
        trainerNameById={trainerNameById}
      />

      <AssignTrainerModal
        open={showAssignModal}
        onClose={() => setShowAssignModal(false)}
        memberId={memberId}
        trainers={trainers}
      />

      <RemoveMemberModal
        open={showDeleteModal}
        onClose={() => setShowDeleteModal(false)}
        onConfirm={handleConfirmDelete}
        pending={deleteMember.isPending}
        memberName={member.name}
      />

      <EditMemberModal
        open={showEditModal}
        onClose={() => setShowEditModal(false)}
        onSave={handleSaveEdit}
        saving={updateMember.isPending}
        form={editForm}
        onChange={setEditForm}
      />
    </div>
  );
}

'use client';

import { useMemo, useState, type ReactNode } from 'react';
import Link from 'next/link';
import { useSession } from 'next-auth/react';
import { toast } from 'sonner';
import { PageHeader, StatusBadge, Modal, ConfirmModal } from '@gym360/ui';
import { Plus, Star, Users, Trophy } from 'lucide-react';
import { useTrainers, useApproveTrainer, useRejectTrainer, useCreateTrainer } from '@/lib/hooks/useTrainers';

const CARD_CLASS = 'rounded-xl border bg-card p-6 shadow-sm block';

/** Owners/managers open the trainer's page; the desk (receptionist) only sees the card. */
function TrainerCardShell({ trainerId, linked, children }: { trainerId: string; linked: boolean; children: ReactNode }) {
  return linked
    ? <Link href={`/trainers/${trainerId}`} className={`${CARD_CLASS} hover:shadow-md transition-shadow`}>{children}</Link>
    : <div className={CARD_CLASS}>{children}</div>;
}

export default function TrainersPage() {
  const { data: session } = useSession();
  // Receptionists can add a coach; approving sign-ups and trainer pages stay owner/manager.
  const canManage = session?.user?.staffSubRole !== 'receptionist';
  const { data: trainers = [], isLoading } = useTrainers();
  const approveTrainer = useApproveTrainer();
  const rejectTrainer = useRejectTrainer();
  const createTrainer = useCreateTrainer();

  const [decisions, setDecisions] = useState<Partial<Record<string, 'approved' | 'rejected'>>>({});
  const [showAddTrainer, setShowAddTrainer] = useState(false);
  const [newTrainer, setNewTrainer] = useState({ name: '', nameAr: '', email: '', phone: '', specializations: '' });
  const [formError, setFormError] = useState<string | null>(null);
  const [rejectTarget, setRejectTarget] = useState<{ id: string; name: string } | null>(null);

  const activeTrainers = useMemo(
    () => trainers.filter((t) => t.status === 'active' || decisions[t.id] === 'approved'),
    [trainers, decisions]
  );
  const pendingTrainers = useMemo(
    () => trainers.filter((t) => t.status === 'pending_approval' && !decisions[t.id]),
    [trainers, decisions]
  );

  const handleApprove = (id: string, name: string) => {
    setDecisions((d) => ({ ...d, [id]: 'approved' }));
    approveTrainer.mutate(id, {
      onSuccess: () => toast.success(`${name} approved.`),
      onError: () => {
        setDecisions((d) => { const next = { ...d }; delete next[id]; return next; });
        toast.error(`Failed to approve ${name}.`);
      },
    });
  };
  const handleReject = (id: string, name: string) => {
    setRejectTarget({ id, name });
  };
  const confirmReject = () => {
    if (!rejectTarget) return;
    const { id, name } = rejectTarget;
    setDecisions((d) => ({ ...d, [id]: 'rejected' }));
    rejectTrainer.mutate({ trainerId: id, reason: 'Rejected by manager' }, {
      onSuccess: () => toast.success(`${name} rejected.`),
      onError: () => {
        setDecisions((d) => { const next = { ...d }; delete next[id]; return next; });
        toast.error(`Failed to reject ${name}.`);
      },
    });
    setRejectTarget(null);
  };
  const handleCreateTrainer = () => {
    if (!newTrainer.name.trim()) {
      setFormError('Name is required.');
      return;
    }
    if (!newTrainer.phone.trim()) {
      setFormError('Phone is required.');
      return;
    }
    // Trainers sign in to the Trainer app with their email.
    if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(newTrainer.email.trim())) {
      setFormError('A valid email is required: the trainer signs in with it.');
      return;
    }
    setFormError(null);
    const name = newTrainer.name;
    createTrainer.mutate(
      { name, email: newTrainer.email.trim(), phone: newTrainer.phone },
      {
        onSuccess: ({ initialPassword }) => {
          toast.success(`${name} added.`, {
            description: initialPassword
              ? `Login: ${newTrainer.email.trim()} / password ${initialPassword}`
              : 'Already has an account — signs in with their current password.',
            duration: 15000,
          });
          setNewTrainer({ name: '', nameAr: '', email: '', phone: '', specializations: '' });
          setShowAddTrainer(false);
        },
        onError: (err: unknown) => {
          const msg = err instanceof Error ? err.message : 'Failed to add trainer.';
          setFormError(msg);
          toast.error(`Failed to add ${name}.`);
        },
      }
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Trainers"
        description={`${trainers.length} trainers at First Gym`}
        actions={
          <button
            onClick={() => setShowAddTrainer(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" /> Add Trainer
          </button>
        }
      />

      {isLoading && (
        <div className="rounded-xl border bg-card p-6 text-center text-sm text-muted-foreground">
          Loading trainers…
        </div>
      )}

      {/* Pending Approval */}
      {canManage && pendingTrainers.length > 0 && (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4">
          <h3 className="text-sm font-semibold text-amber-800 mb-3">Pending Approval ({pendingTrainers.length})</h3>
          {pendingTrainers.map((trainer) => (
            <div key={trainer.id} className="flex items-center justify-between py-2">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-full bg-amber-200 flex items-center justify-center text-sm font-bold text-amber-700">
                  {trainer.name?.split(' ').slice(-1)[0]?.[0]}
                </div>
                <div>
                  <p className="text-sm font-medium text-amber-900">{trainer.name}</p>
                  <p className="text-xs text-amber-700">{trainer.specializations.join(', ')}</p>
                </div>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => handleReject(trainer.id, trainer.name)}
                  className="rounded-lg border border-red-200 bg-white px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50"
                >
                  Reject
                </button>
                <button
                  onClick={() => handleApprove(trainer.id, trainer.name)}
                  className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700"
                >
                  Approve
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Active Trainers Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {activeTrainers.map((trainer) => (
          <TrainerCardShell key={trainer.id} trainerId={trainer.id} linked={canManage}>
            <div className="flex items-start gap-4">
              <div className="h-14 w-14 rounded-full bg-primary/10 flex items-center justify-center text-lg font-bold text-primary shrink-0">
                {trainer.name?.split(' ').slice(-1)[0]?.[0]}
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold truncate">{trainer.name}</h3>
                <p className="text-xs text-muted-foreground truncate">{trainer.nameAr}</p>
                <div className="flex flex-wrap gap-1 mt-2">
                  {trainer.specializations.map((spec) => (
                    <span key={spec} className="text-[10px] bg-primary/10 text-primary px-2 py-0.5 rounded-full">{spec}</span>
                  ))}
                </div>
              </div>
            </div>
            <div className="grid grid-cols-3 gap-2 mt-4 pt-4 border-t">
              <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <Star className="h-3.5 w-3.5 text-[#D4AF37]" fill="#D4AF37" />
                  <span className="text-sm font-bold">{trainer.rating}</span>
                </div>
                <p className="text-[10px] text-muted-foreground">Rating</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <Users className="h-3.5 w-3.5 text-primary" />
                  <span className="text-sm font-bold">{trainer.activeMemberCount}</span>
                </div>
                <p className="text-[10px] text-muted-foreground">Members</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <Trophy className="h-3.5 w-3.5 text-emerald-500" />
                  <span className="text-sm font-bold">{trainer.monthlyScore}</span>
                </div>
                <p className="text-[10px] text-muted-foreground">Score</p>
              </div>
            </div>
          </TrainerCardShell>
        ))}
      </div>

      <Modal
        open={showAddTrainer}
        onClose={() => { setShowAddTrainer(false); setFormError(null); }}
        title="Add Trainer"
        size="md"
        footer={
          <>
            <button onClick={() => { setShowAddTrainer(false); setFormError(null); }} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted" disabled={createTrainer.isPending}>Cancel</button>
            <button onClick={handleCreateTrainer} disabled={createTrainer.isPending} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">{createTrainer.isPending ? 'Adding…' : 'Add Trainer'}</button>
          </>
        }
      >
        <div className="space-y-3">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <div className="grid grid-cols-2 gap-3">
            <label className="block">
              <span className="text-sm font-medium">Name (English)</span>
              <input value={newTrainer.name} onChange={(e) => setNewTrainer({ ...newTrainer, name: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </label>
            <label className="block">
              <span className="text-sm font-medium">Name (Arabic)</span>
              <input value={newTrainer.nameAr} onChange={(e) => setNewTrainer({ ...newTrainer, nameAr: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" dir="rtl" />
            </label>
          </div>
          <label className="block">
            <span className="text-sm font-medium">Email (used to sign in) *</span>
            <input type="email" value={newTrainer.email} onChange={(e) => setNewTrainer({ ...newTrainer, email: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Phone</span>
            <input type="tel" value={newTrainer.phone} onChange={(e) => setNewTrainer({ ...newTrainer, phone: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Specializations (comma-separated)</span>
            <input value={newTrainer.specializations} onChange={(e) => setNewTrainer({ ...newTrainer, specializations: e.target.value })} placeholder="Strength, Hypertrophy, Boxing" className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </label>
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-900">
            <p className="font-semibold">New trainer login password: <span className="font-mono">12345678</span></p>
            <p className="mt-1">Share with the trainer privately. They should change it on first login. A trainer who already has an account keeps their own password.</p>
          </div>
        </div>
      </Modal>

      <ConfirmModal
        open={!!rejectTarget}
        onClose={() => setRejectTarget(null)}
        onConfirm={confirmReject}
        title="Reject Trainer"
        description={`Reject ${rejectTarget?.name ?? 'this trainer'}? This action cannot be undone.`}
        confirmLabel="Reject"
        variant="danger"
      />
    </div>
  );
}

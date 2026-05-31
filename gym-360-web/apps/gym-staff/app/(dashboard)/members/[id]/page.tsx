'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { useSession } from 'next-auth/react';
import { PageHeader, StatusBadge, KpiCard, Modal, formatCurrency, formatDate } from '@gym360/ui';
import { Users, CreditCard, CalendarDays, Activity, Edit, Trash2, UserCheck } from 'lucide-react';
import { useMember, useUpdateMember, useDeleteMember, useQuickAssignTrainer, useMemberPackages } from '@/lib/hooks/useMembers';
import { useTrainers } from '@/lib/hooks/useTrainers';
import { usePayments } from '@/lib/hooks/useBilling';

const SESSION_COUNT_OPTIONS = [4, 8, 12, 16, 20, 24, 28, 32, 36, 40];

const tabs = ['Profile', 'Payments', 'Sessions', 'Progress'] as const;

export default function MemberDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { data: session } = useSession();
  // BE requires gym_owner_or_admin to DELETE members; gate the UI on the same
  // claim so staff (manager/receptionist) don't see a button that would 403.
  const principalType = session?.user?.principalType;
  const adminRole = (session?.user as unknown as { adminRole?: string } | undefined)?.adminRole;
  const isOwnerOrAdmin = principalType === 'Owner' || principalType === 'PlatformAdmin' || !!adminRole;
  const memberId = params.id as string;
  const { data: member, isLoading: memberLoading } = useMember(memberId);
  const { data: payments = [] } = usePayments();
  const updateMember = useUpdateMember();
  const deleteMember = useDeleteMember();
  const quickAssign = useQuickAssignTrainer();
  const { data: trainers = [] } = useTrainers();
  const { data: memberPackages = [], isLoading: packagesLoading } = useMemberPackages(memberId);
  const trainerNameById = new Map(trainers.map((t) => [t.id, t.name] as const));
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Profile');
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [editForm, setEditForm] = useState({ name: '', nameAr: '', email: '', phone: '' });
  const [assignForm, setAssignForm] = useState<{
    trainerId: string;
    sessionCount: number;
    totalPrice: string;
    paymentMethod: string;
  }>({ trainerId: '', sessionCount: 4, totalPrice: '', paymentMethod: 'Cash' });

  const handleConfirmAssign = () => {
    if (!assignForm.trainerId) {
      toast.error('Pick a trainer first.');
      return;
    }
    const priceNum = assignForm.totalPrice.trim() === ''
      ? undefined
      : Number(assignForm.totalPrice);
    if (priceNum !== undefined && (Number.isNaN(priceNum) || priceNum < 0)) {
      toast.error('Price must be a non-negative number.');
      return;
    }
    quickAssign.mutate(
      {
        memberId,
        trainerId: assignForm.trainerId,
        sessionCount: assignForm.sessionCount,
        totalPrice: priceNum,
        // Only attach a payment row when there's an actual price; free
        // assignments stay package-only.
        paymentMethod: priceNum && priceNum > 0 ? assignForm.paymentMethod : undefined,
      },
      {
        onSuccess: () => {
          toast.success(`Assigned ${assignForm.sessionCount} sessions.`);
          setShowAssignModal(false);
          setAssignForm({ trainerId: '', sessionCount: 4, totalPrice: '', paymentMethod: 'Cash' });
        },
        onError: (e: unknown) => {
          const msg = e instanceof Error ? e.message : 'Failed to assign trainer.';
          toast.error(msg);
        },
      },
    );
  };

  const handleConfirmDelete = () => {
    deleteMember.mutate(memberId, {
      onSuccess: () => {
        toast.success('Member removed.');
        setShowDeleteModal(false);
        router.push('/members');
      },
      onError: (e: unknown) => {
        const msg = e instanceof Error ? e.message : 'Failed to remove member.';
        toast.error(msg);
      },
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

  if (!member) {
    return (
      <div className="space-y-6">
        <PageHeader title="Member Not Found" breadcrumbs={[{ label: 'Members', href: '/members' }, { label: 'Not Found' }]} />
        <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
          <p className="text-muted-foreground">No member found with this ID.</p>
        </div>
      </div>
    );
  }

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
          <div className="flex items-center gap-2">
            <button
              onClick={() => setShowAssignModal(true)}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-700"
            >
              <UserCheck className="h-4 w-4" /> Assign Trainer
            </button>
            <button
              onClick={openEdit}
              className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium hover:bg-accent"
            >
              <Edit className="h-4 w-4" /> Edit
            </button>
            {isOwnerOrAdmin && (
              <button
                onClick={() => setShowDeleteModal(true)}
                className="inline-flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100"
              >
                <Trash2 className="h-4 w-4" /> Remove
              </button>
            )}
          </div>
        }
      />

      <div className="flex items-center gap-3">
        <StatusBadge variant={member.status} dot />
        <span className="text-sm text-muted-foreground capitalize">{(member.membershipType ?? '').replace(/_/g, ' ')}</span>
        <span className="text-sm text-muted-foreground">{member.section}</span>
      </div>

      {/* Tabs */}
      <div className="border-b">
        <div className="flex gap-6">
          {tabs.map((tab) => (
            <button key={tab} onClick={() => setActiveTab(tab)} className={`pb-3 text-sm font-medium border-b-2 ${activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'}`}>
              {tab}
            </button>
          ))}
        </div>
      </div>

      {activeTab === 'Profile' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <h3 className="text-sm font-semibold mb-4">Personal Information</h3>
            <dl className="grid grid-cols-2 gap-4 text-sm">
              <div><dt className="text-muted-foreground">Name</dt><dd className="font-medium">{member.name}</dd></div>
              <div><dt className="text-muted-foreground">Arabic Name</dt><dd className="font-medium">{member.nameAr}</dd></div>
              <div><dt className="text-muted-foreground">Email</dt><dd className="font-medium">{member.email}</dd></div>
              <div><dt className="text-muted-foreground">Phone</dt><dd className="font-medium">{member.phone}</dd></div>
              <div><dt className="text-muted-foreground">Floor</dt><dd className="font-medium">{member.section}</dd></div>
              <div><dt className="text-muted-foreground">National ID</dt><dd className="font-medium">{member.nationalId || '—'}</dd></div>
              <div><dt className="text-muted-foreground">Status</dt><dd><StatusBadge variant={member.status} /></dd></div>
            </dl>
          </div>
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <h3 className="text-sm font-semibold mb-4">Membership</h3>
            <dl className="grid grid-cols-2 gap-4 text-sm">
              <div><dt className="text-muted-foreground">Type</dt><dd className="font-medium capitalize">{(member.membershipType ?? '').replace(/_/g, ' ')}</dd></div>
              <div><dt className="text-muted-foreground">Joined</dt><dd className="font-medium">{formatDate(member.joinDate)}</dd></div>
              <div><dt className="text-muted-foreground">Expires</dt><dd className="font-medium">{formatDate(member.expiryDate)}</dd></div>
              <div><dt className="text-muted-foreground">Last Check-in</dt><dd className="font-medium">{member.lastCheckIn ? formatDate(member.lastCheckIn) : 'Never'}</dd></div>
            </dl>
          </div>
        </div>
      )}

      {activeTab === 'Payments' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Payment History</h3>
          <div className="space-y-3">
            {payments.slice(0, 3).map((p) => (
              <div key={p.id} className="flex items-center justify-between py-2 border-b last:border-0">
                <div>
                  <p className="text-sm font-medium capitalize">{p.type?.replace(/_/g, ' ')}</p>
                  <p className="text-xs text-muted-foreground">{formatDate(p.createdAt)} &middot; {p.method?.replace(/_/g, ' ')}</p>
                </div>
                <div className="text-right">
                  <p className="text-sm font-medium">{formatCurrency(p.amount)}</p>
                  <StatusBadge variant={p.status} className="mt-0.5" />
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {activeTab === 'Sessions' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Training Packages</h3>
          {packagesLoading ? (
            <p className="text-sm text-muted-foreground">Loading packages…</p>
          ) : memberPackages.length === 0 ? (
            <p className="text-sm text-muted-foreground">No trainer assigned yet. Use "Assign Trainer" to start a package.</p>
          ) : (
            <div className="space-y-3">
              {memberPackages.map((p) => {
                const used = Math.max(0, p.totalSessions - p.remainingSessions);
                const trainerName = trainerNameById.get(p.trainerId) ?? '—';
                return (
                  <div key={p.id} className="rounded-lg border p-4">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="text-sm font-medium">{p.name}</p>
                        <p className="text-xs text-muted-foreground mt-0.5">
                          Trainer: <span className="font-medium text-foreground">{trainerName}</span>
                        </p>
                        <p className="text-xs text-muted-foreground mt-0.5">
                          Started {p.createdAt ? formatDate(p.createdAt) : '—'}
                          {p.expiresAt ? ` · Expires ${formatDate(p.expiresAt)}` : ''}
                        </p>
                      </div>
                      <div className="text-right">
                        <StatusBadge variant={p.status.toLowerCase()} />
                        <p className="text-sm font-medium mt-1">{formatCurrency(p.totalPrice)}</p>
                      </div>
                    </div>
                    <div className="mt-3 flex items-center gap-3">
                      <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                        <div
                          className="h-full bg-emerald-500"
                          style={{ width: `${p.totalSessions > 0 ? (used / p.totalSessions) * 100 : 0}%` }}
                        />
                      </div>
                      <p className="text-xs font-medium tabular-nums whitespace-nowrap">
                        {used}/{p.totalSessions} used · {p.remainingSessions} left
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {activeTab === 'Progress' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Progress Tracking</h3>
          <p className="text-sm text-muted-foreground">Body metrics, progress photos, and workout history will load here.</p>
        </div>
      )}

      <Modal
        open={showAssignModal}
        onClose={() => !quickAssign.isPending && setShowAssignModal(false)}
        title="Assign Trainer"
        size="md"
        footer={
          <>
            <button
              onClick={() => setShowAssignModal(false)}
              disabled={quickAssign.isPending}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              onClick={handleConfirmAssign}
              disabled={quickAssign.isPending || !assignForm.trainerId}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              {quickAssign.isPending ? 'Assigning…' : 'Assign'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Trainer</label>
            <select
              value={assignForm.trainerId}
              onChange={(e) => setAssignForm({ ...assignForm, trainerId: e.target.value })}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              <option value="">Select a trainer…</option>
              {trainers
                .filter((t) => (t.status as string) !== 'pending_approval' && (t.status as string) !== 'rejected')
                .map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                    {t.specializations?.length ? ` — ${t.specializations.join(', ')}` : ''}
                  </option>
                ))}
            </select>
            {trainers.length === 0 && (
              <p className="text-xs text-muted-foreground mt-1">No trainers approved at this gym yet.</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Sessions</label>
            <select
              value={assignForm.sessionCount}
              onChange={(e) => setAssignForm({ ...assignForm, sessionCount: Number(e.target.value) })}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              {SESSION_COUNT_OPTIONS.map((n) => (
                <option key={n} value={n}>{n} sessions</option>
              ))}
            </select>
            <p className="text-xs text-muted-foreground mt-1">Multiples of 4.</p>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1.5">Total price (EGP)</label>
              <input
                type="number"
                min="0"
                step="0.01"
                placeholder="0 (no payment)"
                value={assignForm.totalPrice}
                onChange={(e) => setAssignForm({ ...assignForm, totalPrice: e.target.value })}
                className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1.5">Payment method</label>
              <select
                value={assignForm.paymentMethod}
                onChange={(e) => setAssignForm({ ...assignForm, paymentMethod: e.target.value })}
                disabled={!assignForm.totalPrice || Number(assignForm.totalPrice) === 0}
                className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm disabled:opacity-50"
              >
                <option value="Cash">Cash</option>
                <option value="Card">Card</option>
                <option value="BankTransfer">Bank Transfer</option>
                <option value="MobileWallet">Mobile Wallet</option>
              </select>
            </div>
          </div>
          <p className="text-xs text-muted-foreground">
            Total package price for all sessions. Leave blank for a free assignment. Setting a price records a Payment in the same transaction.
          </p>
        </div>
      </Modal>

      <Modal
        open={showDeleteModal}
        onClose={() => !deleteMember.isPending && setShowDeleteModal(false)}
        title="Remove Member"
        size="sm"
        footer={
          <>
            <button
              onClick={() => setShowDeleteModal(false)}
              disabled={deleteMember.isPending}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              onClick={handleConfirmDelete}
              disabled={deleteMember.isPending}
              className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
            >
              {deleteMember.isPending ? 'Removing…' : 'Remove'}
            </button>
          </>
        }
      >
        <p className="text-sm">
          Remove <span className="font-medium">{member.name}</span> from this gym? Active memberships are terminated. This cannot be undone.
        </p>
      </Modal>

      <Modal
        open={showEditModal}
        onClose={() => setShowEditModal(false)}
        title="Edit Profile"
        size="md"
        footer={
          <>
            <button onClick={() => setShowEditModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={handleSaveEdit} disabled={updateMember.isPending} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">
              {updateMember.isPending ? 'Saving…' : 'Save Changes'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1.5">Name (English)</label>
              <input value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1.5">Name (Arabic)</label>
              <input value={editForm.nameAr} onChange={(e) => setEditForm({ ...editForm, nameAr: e.target.value })} dir="rtl" className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Email</label>
            <input type="email" value={editForm.email} onChange={(e) => setEditForm({ ...editForm, email: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Phone</label>
            <input type="tel" value={editForm.phone} onChange={(e) => setEditForm({ ...editForm, phone: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </div>
        </div>
      </Modal>
    </div>
  );
}

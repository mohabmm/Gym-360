'use client';

import { useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import { useSession } from 'next-auth/react';
import { toast } from 'sonner';
import { PageHeader, StatusBadge, KpiCard } from '@gym360/ui';
import { Star, Users, Edit, DollarSign, X, Save, Mail, Phone } from 'lucide-react';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';
import {
  useTrainer,
  useTrainers,
  useTrainerClients,
  useTrainerSessions,
  useUpdateTrainerProfile,
} from '@/lib/hooks/useTrainers';

const tabs = ['Profile', 'Members', 'Sessions', 'Ratings', 'Earnings'] as const;

// --- API helpers ---
async function fetchTrainerRate(gymId: string, trainerId: string) {
  try {
    const res = await apiClient.get(`/gyms/${gymId}/trainers/${trainerId}/rate`);
    return res.data?.data as { ratePerSession: number; currency: string; effectiveFrom: string } | null;
  } catch (e) {
    const status = (e as { response?: { status?: number } })?.response?.status;
    if (status === 404) return null;
    throw e;
  }
}

async function setTrainerRate(gymId: string, trainerId: string, rate: number) {
  // setByUserId derived from JWT on the backend.
  await apiClient.put(`/gyms/${gymId}/trainers/${trainerId}/rate`, {
    ratePerSession: rate,
  });
}

async function fetchTrainerEarnings(gymId: string, trainerId: string) {
  const now = new Date();
  const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1)
    .toISOString()
    .split('T')[0];
  const today = now.toISOString().split('T')[0];
  const res = await apiClient.get(`/billing/trainers/${trainerId}/earnings`, {
    params: { gymId, from: firstOfMonth, to: today },
  });
  return res.data?.data as {
    totalSessions: number;
    totalEarningsEgp: number;
    currency: string;
    periodFrom: string;
    periodTo: string;
    records: Array<{ sessionId: string; memberId: string; rateApplied: number; earnedAt: string }>;
  };
}

// --- Edit Profile Modal ---
function EditProfileModalForm({
  open,
  onClose,
  initialBio,
  initialSpecializations,
  onSave,
  isSaving,
}: {
  open: boolean;
  onClose: () => void;
  initialBio: string;
  initialSpecializations: string[];
  onSave: (payload: { bio: string; specializations: string[] }) => void;
  isSaving: boolean;
}) {
  const [bio, setBio] = useState(initialBio);
  const [specsCsv, setSpecsCsv] = useState(initialSpecializations.join(', '));

  // reset when reopened
  const lastOpenRef = useRef(false);
  if (open && !lastOpenRef.current) {
    lastOpenRef.current = true;
    setBio(initialBio);
    setSpecsCsv(initialSpecializations.join(', '));
  } else if (!open && lastOpenRef.current) {
    lastOpenRef.current = false;
  }

  if (!open) return null;

  const specializations = specsCsv
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-md rounded-xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-heading font-semibold text-lg">Edit Trainer Profile</h3>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="space-y-3">
          <label className="block">
            <span className="text-sm font-medium">Specializations</span>
            <input
              value={specsCsv}
              onChange={(e) => setSpecsCsv(e.target.value)}
              placeholder="e.g. Strength, HIIT, Mobility"
              className="mt-1 w-full h-9 rounded-lg border border-border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
            <span className="text-xs text-muted-foreground mt-1 block">Comma-separated.</span>
          </label>
          <label className="block">
            <span className="text-sm font-medium">Bio</span>
            <textarea
              value={bio}
              onChange={(e) => setBio(e.target.value)}
              rows={4}
              placeholder="Short trainer bio…"
              className="mt-1 w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </label>
        </div>
        <div className="flex gap-3 justify-end mt-5">
          <button
            onClick={onClose}
            className="rounded-lg border border-border px-4 py-2 text-sm font-medium hover:bg-muted transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={() => onSave({ bio, specializations })}
            disabled={isSaving}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50 transition-colors"
          >
            <Save className="h-4 w-4" />
            {isSaving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      </div>
    </div>
  );
}

// --- Edit Rate Modal ---
function EditRateModal({
  currentRate,
  onClose,
  onSave,
  isSaving,
}: {
  currentRate: number | null;
  onClose: () => void;
  onSave: (rate: number) => void;
  isSaving: boolean;
}) {
  const [value, setValue] = useState(currentRate?.toString() ?? '');
  const parsed = parseFloat(value);
  const isValid = !isNaN(parsed) && parsed >= 0 && parsed <= 10000;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-sm rounded-xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-heading font-semibold text-lg">Edit Session Rate</h3>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>
        <p className="text-sm text-muted-foreground mb-4">
          Set the flat per-session rate for this trainer in EGP.
        </p>
        <div className="relative mb-4">
          <input
            type="number"
            min={0}
            max={10000}
            step={1}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="e.g. 150"
            className="w-full rounded-lg border border-border bg-background px-4 py-2.5 pr-14 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
          />
          <span className="absolute right-4 top-1/2 -translate-y-1/2 text-sm font-medium text-muted-foreground">
            EGP
          </span>
        </div>
        {value !== '' && !isValid && (
          <p className="text-xs text-destructive mb-3">Rate must be between 0 and 10,000 EGP.</p>
        )}
        <div className="flex gap-3 justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={() => isValid && onSave(parsed)}
            disabled={!isValid || isSaving}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
          >
            <Save className="h-4 w-4" />
            {isSaving ? 'Saving…' : 'Save Rate'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default function TrainerDetailPage() {
  const params = useParams();
  const trainerId = params.id as string;
  const { data: session } = useSession();
  const gymId = session?.user?.gymId ?? '';

  const isAlias = /^t\d+$/i.test(trainerId);
  const aliasIndex = isAlias ? Math.max(0, parseInt(trainerId.slice(1), 10) - 1) : -1;
  const { data: trainersList } = useTrainers();
  const { data: trainerDirect, isLoading: trainerLoading } = useTrainer(trainerId);
  const trainer = isAlias ? (trainersList?.[aliasIndex] ?? trainersList?.[0]) : trainerDirect;
  const isLoading = isAlias ? !trainersList : trainerLoading;

  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Profile');
  const [editRateOpen, setEditRateOpen] = useState(false);
  const [editProfileOpen, setEditProfileOpen] = useState(false);
  const queryClient = useQueryClient();

  const { data: rateData, isLoading: rateLoading } = useQuery({
    queryKey: ['trainer-rate', gymId, trainerId],
    queryFn: () => fetchTrainerRate(gymId, trainerId),
    enabled: !!gymId && !!trainerId,
  });

  const { data: earningsData, isLoading: earningsLoading } = useQuery({
    queryKey: ['trainer-earnings', gymId, trainerId],
    queryFn: () => fetchTrainerEarnings(gymId, trainerId),
    enabled: activeTab === 'Earnings' && !!gymId && !!trainerId,
  });

  const resolvedTrainerId = trainer?.id ?? trainerId;
  const { data: clientsData, isLoading: clientsLoading } = useTrainerClients(
    activeTab === 'Members' ? resolvedTrainerId : '',
  );
  const { data: sessionsData, isLoading: sessionsLoading } = useTrainerSessions(
    activeTab === 'Sessions' ? resolvedTrainerId : '',
  );
  const updateProfileMutation = useUpdateTrainerProfile(resolvedTrainerId);

  const setRateMutation = useMutation({
    mutationFn: (rate: number) => setTrainerRate(gymId, trainerId, rate),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trainer-rate', gymId, trainerId] });
      setEditRateOpen(false);
    },
  });

  if (isLoading) {
    return (
      <div className="text-center py-20">
        <p className="text-sm text-muted-foreground">Loading trainer…</p>
      </div>
    );
  }

  if (!trainer) {
    return (
      <div className="text-center py-20">
        <h2 className="text-lg font-semibold">Trainer not found</h2>
        <Link href="/trainers" className="text-primary text-sm mt-2 inline-block hover:underline">
          Back to Trainers
        </Link>
      </div>
    );
  }

  const initials = (trainer.name ?? '').split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2);
  const currentRate = rateData?.ratePerSession ?? null;

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
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <div className="flex items-start gap-5">
          <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary font-heading font-bold text-xl">
            {initials}
          </div>
          <div className="flex-1">
            <div className="flex items-center gap-3 flex-wrap">
              <h2 className="text-xl font-heading font-bold">{trainer.name}</h2>
              <StatusBadge variant={trainer.status} />
            </div>
            <p className="text-muted-foreground text-sm mt-1">{trainer.nameAr}</p>
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2 text-sm">
              <span className="inline-flex items-center gap-1.5 text-muted-foreground">
                <Mail className="h-3.5 w-3.5" />
                {trainer.email ? (
                  <a href={`mailto:${trainer.email}`} className="hover:text-primary hover:underline">
                    {trainer.email}
                  </a>
                ) : (
                  <span className="italic">No email on file</span>
                )}
              </span>
              {trainer.phone && (
                <span className="inline-flex items-center gap-1.5 text-muted-foreground">
                  <Phone className="h-3.5 w-3.5" />
                  <a href={`tel:${trainer.phone}`} className="hover:text-primary hover:underline">
                    {trainer.phone}
                  </a>
                </span>
              )}
            </div>
            <div className="flex flex-wrap gap-2 mt-3">
              {trainer.specializations.map((spec) => (
                <span
                  key={spec}
                  className="px-2.5 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium"
                >
                  {spec}
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <KpiCard
          value={trainer.activeMemberCount.toString()}
          label="Active Clients"
          icon={<Users className="h-5 w-5" />}
          variant="default"
        />
        <KpiCard
          value={trainer.monthlyScore.toString()}
          label="Monthly Score"
          icon={<Star className="h-5 w-5" />}
          variant="info"
        />
        <KpiCard
          value={`${trainer.rating}/5`}
          label="Average Rating"
          icon={<Star className="h-5 w-5" />}
          variant="success"
        />
        <KpiCard
          value={rateLoading ? '…' : currentRate !== null ? `${currentRate} EGP` : 'Not set'}
          label="Session Rate"
          icon={<DollarSign className="h-5 w-5" />}
          variant="default"
        />
      </div>

      {/* Tabs */}
      <div className="border-b border-border">
        <div className="flex gap-1">
          {tabs.map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-5 py-3 text-sm font-medium border-b-2 -mb-px transition-colors ${
                activeTab === tab
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              {tab}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      {activeTab === 'Profile' && (
        <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
          <h3 className="font-heading font-semibold mb-4">Trainer Information</h3>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-y-4 gap-x-6 text-sm">
            <div>
              <dt className="text-muted-foreground font-medium">Status</dt>
              <dd className="mt-1">
                <StatusBadge variant={trainer.status} />
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground font-medium">Email</dt>
              <dd className="mt-1 font-medium break-all">
                {trainer.email ? (
                  <a href={`mailto:${trainer.email}`} className="hover:text-primary hover:underline">
                    {trainer.email}
                  </a>
                ) : (
                  <span className="text-muted-foreground italic">Not provided</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground font-medium">Phone</dt>
              <dd className="mt-1 font-medium">
                {trainer.phone ? (
                  <a href={`tel:${trainer.phone}`} className="hover:text-primary hover:underline">
                    {trainer.phone}
                  </a>
                ) : (
                  <span className="text-muted-foreground italic">Not provided</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground font-medium">Active Clients</dt>
              <dd className="mt-1 font-medium">{trainer.activeMemberCount}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground font-medium">Monthly Score</dt>
              <dd className="mt-1 font-medium">{trainer.monthlyScore.toFixed(1)}/5</dd>
            </div>
            <div>
              <dt className="text-muted-foreground font-medium">Rating</dt>
              <dd className="mt-1 flex items-center gap-1.5 font-medium">
                <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
                {trainer.rating}/5.0
              </dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-muted-foreground font-medium">Specializations</dt>
              <dd className="mt-2 flex flex-wrap gap-2">
                {trainer.specializations.length > 0 ? (
                  trainer.specializations.map((s) => (
                    <span key={s} className="px-2.5 py-0.5 rounded-full bg-accent text-accent-foreground text-xs font-medium border">
                      {s}
                    </span>
                  ))
                ) : (
                  <span className="text-muted-foreground italic text-xs">None listed.</span>
                )}
              </dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-muted-foreground font-medium">Bio</dt>
              <dd className="mt-1 whitespace-pre-line leading-relaxed">
                {trainer.bio?.trim() ? (
                  trainer.bio
                ) : (
                  <span className="text-muted-foreground italic">No bio provided.</span>
                )}
              </dd>
            </div>
          </dl>
        </div>
      )}

      {activeTab === 'Members' && (
        <div className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
          <div className="flex items-center justify-between p-6 border-b border-border">
            <div>
              <h3 className="font-heading font-semibold">Assigned Clients</h3>
              <p className="text-xs text-muted-foreground mt-1">
                {trainer.activeMemberCount} active client{trainer.activeMemberCount === 1 ? '' : 's'} training with {trainer.name}
              </p>
            </div>
          </div>
          {clientsLoading ? (
            <div className="p-10 text-center text-sm text-muted-foreground">Loading clients…</div>
          ) : !clientsData || clientsData.length === 0 ? (
            <div className="p-10 text-center text-sm text-muted-foreground">No clients assigned yet.</div>
          ) : (
            <div className="divide-y divide-border">
              {clientsData.map((c) => (
                <Link
                  key={c.id}
                  href={`/members/${c.id}`}
                  className="flex items-center justify-between px-6 py-4 hover:bg-muted/30 transition-colors"
                >
                  <div>
                    <p className="font-medium text-sm">{c.name}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {c.memberNumber ? `#${c.memberNumber}` : ''}
                      {c.memberNumber && (c.email || c.phone) ? ' · ' : ''}
                      {c.email ?? c.phone ?? ''}
                    </p>
                  </div>
                  <span className="text-xs text-muted-foreground">
                    Joined {new Date(c.joinedAt).toLocaleDateString()}
                  </span>
                </Link>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === 'Sessions' && (
        <div className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
          <div className="p-6 border-b border-border">
            <h3 className="font-heading font-semibold">Session History</h3>
            <p className="text-xs text-muted-foreground mt-1">Sessions completed and upcoming for this trainer</p>
          </div>
          {sessionsLoading ? (
            <div className="p-10 text-center text-sm text-muted-foreground">Loading sessions…</div>
          ) : !sessionsData || sessionsData.length === 0 ? (
            <div className="p-10 text-center text-sm text-muted-foreground">No sessions recorded yet.</div>
          ) : (
            <div className="divide-y divide-border">
              {sessionsData.map((s) => (
                <div key={s.id} className="flex items-center justify-between px-6 py-3 text-sm">
                  <div>
                    <p className="font-medium">{new Date(s.scheduledStart).toLocaleString()}</p>
                    <p className="text-xs text-muted-foreground mt-0.5 font-mono">
                      Member {s.memberId.slice(0, 8)}…{s.type ? ` · ${s.type}` : ''}
                    </p>
                  </div>
                  <span className="text-xs px-2 py-0.5 rounded-full bg-muted">{s.status}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === 'Ratings' && (
        <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
          <div className="flex items-center justify-between mb-5 flex-wrap gap-2">
            <h3 className="font-heading font-semibold">Client Ratings</h3>
            <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 px-2.5 py-1 rounded-lg">
              Manager eyes only — trainers see aggregated scores only
            </p>
          </div>
          <div className="p-10 text-center text-sm text-muted-foreground">
            No ratings submitted yet.
          </div>
        </div>
      )}

      {activeTab === 'Earnings' && (
        <div className="space-y-4">
          {/* Rate configuration card */}
          <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-heading font-semibold">Session Rate</h3>
              <button
                onClick={() => setEditRateOpen(true)}
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium hover:bg-muted transition-colors"
              >
                <Edit className="h-4 w-4" /> Edit Rate
              </button>
            </div>
            {rateLoading ? (
              <p className="text-sm text-muted-foreground">Loading rate…</p>
            ) : currentRate !== null ? (
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-heading font-bold text-primary">{currentRate}</span>
                <span className="text-lg font-medium text-muted-foreground">EGP / session</span>
              </div>
            ) : (
              <div className="flex items-center gap-3 text-sm text-muted-foreground">
                <span>No rate configured.</span>
                <button
                  onClick={() => setEditRateOpen(true)}
                  className="text-primary hover:underline font-medium"
                >
                  Set rate now
                </button>
              </div>
            )}
            {rateData?.effectiveFrom && (
              <p className="text-xs text-muted-foreground mt-2">
                Effective from {new Date(rateData.effectiveFrom).toLocaleDateString()}
              </p>
            )}
          </div>

          {/* Monthly earnings summary */}
          <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
            <h3 className="font-heading font-semibold mb-4">Earnings This Month</h3>
            {earningsLoading ? (
              <p className="text-sm text-muted-foreground">Loading earnings…</p>
            ) : earningsData ? (
              <>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
                  <div className="rounded-lg bg-muted/40 p-4">
                    <p className="text-xs text-muted-foreground font-medium">Total Sessions</p>
                    <p className="text-2xl font-heading font-bold mt-1">{earningsData.totalSessions}</p>
                  </div>
                  <div className="rounded-lg bg-muted/40 p-4">
                    <p className="text-xs text-muted-foreground font-medium">Total Earned</p>
                    <p className="text-2xl font-heading font-bold mt-1 text-primary">
                      {earningsData.totalEarningsEgp.toLocaleString()} EGP
                    </p>
                  </div>
                </div>

                {earningsData.records.length > 0 ? (
                  <div>
                    <h4 className="text-sm font-medium text-muted-foreground mb-3">Session Breakdown</h4>
                    <div className="divide-y divide-border rounded-lg border border-border overflow-hidden">
                      {earningsData.records.map((record) => (
                        <div
                          key={record.sessionId}
                          className="flex items-center justify-between px-4 py-3 text-sm hover:bg-muted/20 transition-colors"
                        >
                          <div>
                            <p className="font-medium font-mono text-xs text-muted-foreground">
                              Session {record.sessionId.slice(0, 8)}…
                            </p>
                            <p className="text-xs text-muted-foreground">
                              {new Date(record.earnedAt).toLocaleDateString()}
                            </p>
                          </div>
                          <span className="font-semibold text-primary">{record.rateApplied} EGP</span>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">No sessions completed this month.</p>
                )}
              </>
            ) : (
              <p className="text-sm text-muted-foreground">Failed to load earnings data.</p>
            )}
          </div>
        </div>
      )}

      <EditProfileModalForm
        open={editProfileOpen}
        onClose={() => setEditProfileOpen(false)}
        initialBio={trainer.bio ?? ''}
        initialSpecializations={trainer.specializations ?? []}
        onSave={async (payload) => {
          try {
            await updateProfileMutation.mutateAsync(payload);
            toast.success('Profile saved.');
            setEditProfileOpen(false);
          } catch (e) {
            const msg = (e as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message;
            toast.error(msg ?? 'Failed to save profile.');
          }
        }}
        isSaving={updateProfileMutation.isPending}
      />
    </div>
  );
}

'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import {
  PageHeader,
  StatusBadge,
  KpiCard,
  ConfirmModal,
  Modal,
  formatCurrency,
  formatNumber,
  formatDate,
  apiPost,
  apiPut,
  apiPatch,
  apiDelete,
  GYMS as GYMS_ENDPOINTS,
  BILLING,
  useInvalidateQueries,
} from '@gym360/ui';
import { Building2, Users, DollarSign, Dumbbell, Edit, Pause, Play, Settings, BarChart3, CreditCard, AlertCircle, UserPlus, Shield, Trash2 } from 'lucide-react';
import { useGym, usePlans, useFeatures, usePlanFeatureMatrix, useGymFeatureOverrides, useSuspendGym, useDeleteGym, useGymMembers, useGymTrainers, useGymStaff, useCreateGymStaff, type CreateGymStaffPayload } from '@/lib/hooks';
import { extractApiErrors } from '@gym360/ui';
import { PageSkeleton } from '@gym360/ui';
import { GymSettingsPanel } from '@/components/gym-settings-panel';
import { GymRevenuePanel } from '@/components/gym-revenue-panel';

const tabs = ['Overview', 'Members', 'Trainers', 'Staff', 'Revenue', 'Subscription & Features', 'Settings'] as const;

const EMPTY_STAFF_FORM: CreateGymStaffPayload = {
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phone: '',
  staffSubRole: 'Receptionist',
};

export default function GymDetailPage() {
  const params = useParams();
  const router = useRouter();
  const gymId = params.id as string;
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Overview');
  const [showSuspendModal, setShowSuspendModal] = useState(false);
  const [suspendReason, setSuspendReason] = useState('');
  const [showReactivateModal, setShowReactivateModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deleteConfirmName, setDeleteConfirmName] = useState('');
  const [showEditModal, setShowEditModal] = useState(false);
  const [showChangePlanModal, setShowChangePlanModal] = useState(false);
  const [changePlanReason, setChangePlanReason] = useState('');
  const [editForm, setEditForm] = useState({ name: '', ownerName: '' });
  const [targetPlanId, setTargetPlanId] = useState<string>('');
  const [busyOverrideCode, setBusyOverrideCode] = useState<string | null>(null);
  const [busyAction, setBusyAction] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const { invalidate } = useInvalidateQueries();
  const suspendGym = useSuspendGym();
  const deleteGym = useDeleteGym();

  const { data: gymData, isLoading } = useGym(gymId);
  const { data: plansData } = usePlans();
  const { data: featuresData } = useFeatures();
  const { data: matrixData } = usePlanFeatureMatrix();
  const { data: overridesData } = useGymFeatureOverrides(gymId);
  const { data: membersData, isLoading: membersLoading } = useGymMembers(activeTab === 'Members' ? gymId : '');
  const { data: trainersData, isLoading: trainersLoading } = useGymTrainers(activeTab === 'Trainers' ? gymId : '');
  const { data: staffData, isLoading: staffLoading } = useGymStaff(activeTab === 'Staff' ? gymId : '');
  const createStaffMutation = useCreateGymStaff(gymId);
  const [showStaffModal, setShowStaffModal] = useState(false);
  const [staffForm, setStaffForm] = useState<CreateGymStaffPayload>(EMPTY_STAFF_FORM);
  const [staffError, setStaffError] = useState<string | null>(null);

  const updateStaffField = <K extends keyof CreateGymStaffPayload>(key: K, value: CreateGymStaffPayload[K]) => {
    setStaffForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmitStaff = async (e: React.FormEvent) => {
    e.preventDefault();
    setStaffError(null);
    try {
      await createStaffMutation.mutateAsync(staffForm);
      setShowStaffModal(false);
      setStaffForm(EMPTY_STAFF_FORM);
    } catch (err) {
      const apiErrors = extractApiErrors(err);
      setStaffError(apiErrors[0]?.message ?? 'Failed to create staff member.');
    }
  };

  const closeStaffModal = () => {
    if (createStaffMutation.isPending) return;
    setShowStaffModal(false);
    setStaffForm(EMPTY_STAFF_FORM);
    setStaffError(null);
  };

  const gym = gymData?.data;
  const matrix = matrixData?.data;
  const subscriptionPlans = matrix?.plans ?? plansData?.data ?? [];
  const features = matrix?.features ?? featuresData?.data ?? [];
  // Matrix lookup: planId -> featureId -> isEnabled (backend now keys on ids, not codes).
  const featureMatrix = (matrix?.matrix ?? []).reduce<Record<string, Record<string, boolean>>>(
    (acc, pf) => {
      if (!acc[pf.planId]) acc[pf.planId] = {};
      acc[pf.planId]![pf.featureId] = pf.isEnabled;
      return acc;
    },
    {}
  );
  const gymOverrides = overridesData?.data ?? [];

  if (isLoading) return <PageSkeleton />;
  if (!gym) return <div className="p-6 text-muted-foreground">Gym not found</div>;

  const planName = gym.planName ?? '';
  const planNameLower = planName.toLowerCase();
  const planLabel = planName || 'No Plan';
  const ownerName = gym.ownerName || '—';
  const plan = planName ? subscriptionPlans.find((p) => p.name === planName) : undefined;

  const openEditModal = () => {
    setEditForm({ name: gym.name, ownerName: gym.ownerName ?? '' });
    setShowEditModal(true);
  };

  const handleSaveEdit = async () => {
    setBusyAction(true);
    setActionError(null);
    try {
      await apiPut(GYMS_ENDPOINTS.UPDATE(gymId), editForm);
      invalidate(['gyms', gymId]);
      invalidate(['gyms']);
      setShowEditModal(false);
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : 'Failed to save gym details');
    } finally {
      setBusyAction(false);
    }
  };

  const handleReactivate = async () => {
    setBusyAction(true);
    setActionError(null);
    try {
      await apiPost(GYMS_ENDPOINTS.REACTIVATE(gymId), {});
      invalidate(['gyms', gymId]);
      invalidate(['gyms']);
      setShowReactivateModal(false);
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : 'Failed to reactivate gym');
    } finally {
      setBusyAction(false);
    }
  };

  const openChangePlanModal = () => {
    setTargetPlanId(plan?.id ?? '');
    setChangePlanReason('');
    setShowChangePlanModal(true);
  };

  const handleChangePlan = async () => {
    if (!targetPlanId || !changePlanReason.trim()) return;
    setBusyAction(true);
    setActionError(null);
    try {
      await apiPut(BILLING.CHANGE_PLAN(gymId), { planId: targetPlanId, reason: changePlanReason.trim() });
      invalidate(['subscriptions']);
      invalidate(['gyms', gymId]);
      invalidate(['plan-change-log']);
      setShowChangePlanModal(false);
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : 'Failed to change plan');
    } finally {
      setBusyAction(false);
    }
  };

  const handleDelete = async () => {
    if (deleteConfirmName.trim().toLowerCase() !== (gym?.name ?? '').trim().toLowerCase()) return;
    setBusyAction(true);
    setActionError(null);
    try {
      await deleteGym.mutateAsync({ gymId });
      invalidate(['gyms']);
      setShowDeleteModal(false);
      setDeleteConfirmName('');
      router.push('/gyms');
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : 'Failed to delete gym');
    } finally {
      setBusyAction(false);
    }
  };

  const handleSuspend = async () => {
    if (!suspendReason.trim()) return;
    setBusyAction(true);
    try {
      await suspendGym.mutateAsync({ gymId, reason: suspendReason.trim() });
      invalidate(['gyms', gymId]);
      setShowSuspendModal(false);
      setSuspendReason('');
    } finally {
      setBusyAction(false);
    }
  };

  const handleToggleOverride = async (
    featureId: string,
    featureCode: string,
    existingOverrideId: string | undefined,
    planDefault: boolean,
  ) => {
    setBusyOverrideCode(featureCode);
    try {
      if (existingOverrideId) {
        await apiDelete(BILLING.GYM_OVERRIDE(gymId, featureId));
      } else {
        // Backend expects { featureId, enabled: bool, reason? }. SetBy is derived from JWT.
        // If plan default is ON we force OFF, and vice versa.
        await apiPost(BILLING.GYM_OVERRIDES(gymId), {
          featureId,
          enabled: !planDefault,
          reason: null,
        });
      }
      invalidate(['overrides', gymId]);
    } finally {
      setBusyOverrideCode(null);
    }
  };

  return (
    <div className="space-y-6">
      {actionError && (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-3 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">{actionError}</p>
          <button onClick={() => setActionError(null)} className="ml-auto text-red-400 hover:text-red-600 text-lg leading-none">×</button>
        </div>
      )}
      <PageHeader
        title={gym.name}
        description={`${gym.address?.city ?? gym.gymCode} | Code: ${gym.gymCode} | Owner: ${ownerName}`}
        breadcrumbs={[{ label: 'Gyms', href: '/gyms' }, { label: gym.name }]}
        actions={
          <div className="flex items-center gap-2">
            <button
              onClick={openEditModal}
              className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium hover:bg-accent"
            >
              <Edit className="h-4 w-4" /> Edit
            </button>
            {gym.status === 'active' && (
              <button
                onClick={() => setShowSuspendModal(true)}
                className="inline-flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100"
              >
                <Pause className="h-4 w-4" /> Suspend
              </button>
            )}
            {gym.status === 'suspended' && (
              <button
                onClick={() => setShowReactivateModal(true)}
                className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-700"
              >
                <Play className="h-4 w-4" /> Reactivate
              </button>
            )}
            <button
              onClick={() => { setDeleteConfirmName(''); setShowDeleteModal(true); }}
              className="inline-flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 dark:border-red-800 dark:bg-red-950/30 dark:text-red-400"
            >
              <Trash2 className="h-4 w-4" /> Delete
            </button>
          </div>
        }
      />

      {/* Status badges row */}
      <div className="flex items-center gap-3">
        <StatusBadge variant={gym.status} dot label={`Status: ${gym.status}`} />
        <StatusBadge variant={planNameLower || 'inactive'} label={`Plan: ${planLabel}`} />
        <StatusBadge variant={gym.paymentStatus} label={`Payment: ${gym.paymentStatus}`} />
      </div>

      {/* Tabs */}
      <div className="border-b">
        <div className="flex gap-6">
          {tabs.map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`pb-3 text-sm font-medium transition-colors border-b-2 ${
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
      {activeTab === 'Overview' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <KpiCard value={formatNumber(gym.activeMemberCount)} label="Active Members" icon={<Users className="h-5 w-5" />} variant="success" />
            <KpiCard value={formatNumber(gym.activeTrainerCount)} label="Active Trainers" icon={<Dumbbell className="h-5 w-5" />} variant="info" />
            <KpiCard value={formatCurrency(gym.monthlyRevenue ?? 0)} label="Monthly Revenue" icon={<DollarSign className="h-5 w-5" />} />
            <KpiCard value={gym.subscriptionStatus || '—'} label="Subscription Status" icon={<Building2 className="h-5 w-5" />} />
          </div>
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="rounded-xl border bg-card p-6 shadow-sm">
              <h3 className="text-sm font-semibold mb-4">Gym Details</h3>
              <dl className="grid grid-cols-2 gap-4 text-sm">
                <div><dt className="text-muted-foreground">Gym Code</dt><dd className="font-medium">{gym.gymCode}</dd></div>
                <div><dt className="text-muted-foreground">City</dt><dd className="font-medium">{gym.address?.city ?? gym.gymCode}</dd></div>
                <div><dt className="text-muted-foreground">Owner</dt><dd className="font-medium">{ownerName}</dd></div>
                <div><dt className="text-muted-foreground">Joined</dt><dd className="font-medium">{formatDate(gym.createdAt)}</dd></div>
                <div><dt className="text-muted-foreground">Plan</dt><dd><StatusBadge variant={planNameLower || 'inactive'} label={planLabel} /></dd></div>
                <div><dt className="text-muted-foreground">Payment</dt><dd><StatusBadge variant={gym.paymentStatus} /></dd></div>
              </dl>
            </div>
            <div className="rounded-xl border bg-card p-6 shadow-sm">
              <h3 className="text-sm font-semibold mb-4">Quick Stats</h3>
              <div className="space-y-3">
                <div className="flex items-center justify-between py-2 border-b">
                  <span className="text-sm text-muted-foreground">Active Members</span>
                  <span className="text-sm font-medium">{gym.activeMemberCount}</span>
                </div>
                <div className="flex items-center justify-between py-2 border-b">
                  <span className="text-sm text-muted-foreground">Active Trainers</span>
                  <span className="text-sm font-medium">{gym.activeTrainerCount}</span>
                </div>
                <div className="flex items-center justify-between py-2 border-b">
                  <span className="text-sm text-muted-foreground">Monthly Revenue</span>
                  <span className="text-sm font-medium">{formatCurrency(gym.monthlyRevenue ?? 0)}</span>
                </div>
                <div className="flex items-center justify-between py-2">
                  <span className="text-sm text-muted-foreground">Active Subscription</span>
                  <span className="text-sm font-medium">{plan?.name ?? '—'}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'Members' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Members ({gym.activeMemberCount})</h3>
          {membersLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="h-10 bg-muted animate-pulse rounded-lg" />
              ))}
            </div>
          ) : (membersData?.data ?? []).length === 0 ? (
            <p className="text-sm text-muted-foreground">No members found for this gym.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Name</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Email</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Membership</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Status</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Joined</th>
                  </tr>
                </thead>
                <tbody>
                  {(membersData?.data ?? []).map((m) => (
                    <tr key={m.id} className="border-b last:border-0 hover:bg-muted/50">
                      <td className="py-2 px-3 font-medium">{m.name}</td>
                      <td className="py-2 px-3 text-muted-foreground">{m.email}</td>
                      <td className="py-2 px-3 capitalize">{m.membershipType?.replace('_', ' ')}</td>
                      <td className="py-2 px-3">
                        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          m.status === 'active' ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'
                        }`}>{m.status}</span>
                      </td>
                      <td className="py-2 px-3 text-muted-foreground">{m.joinDate ? new Date(m.joinDate).toLocaleDateString() : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'Trainers' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Trainers ({gym.activeTrainerCount})</h3>
          {trainersLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="h-10 bg-muted animate-pulse rounded-lg" />
              ))}
            </div>
          ) : (trainersData?.data ?? []).length === 0 ? (
            <p className="text-sm text-muted-foreground">No trainers found for this gym.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Name</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Specializations</th>
                    <th className="text-right py-2 px-3 font-semibold text-muted-foreground">Active Clients</th>
                    <th className="text-right py-2 px-3 font-semibold text-muted-foreground">Rating</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {(trainersData?.data ?? []).map((t) => (
                    <tr key={t.id} className="border-b last:border-0 hover:bg-muted/50">
                      <td className="py-2 px-3 font-medium">{t.name}</td>
                      <td className="py-2 px-3 text-muted-foreground">{(t.specializations ?? []).join(', ') || '—'}</td>
                      <td className="py-2 px-3 text-right">{t.activeMemberCount}</td>
                      <td className="py-2 px-3 text-right">{t.rating ? t.rating.toFixed(1) : '—'}</td>
                      <td className="py-2 px-3">
                        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          t.status === 'active' ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'
                        }`}>{t.status}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'Staff' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold flex items-center gap-2">
              <Shield className="h-4 w-4" />
              Staff ({(staffData?.data ?? []).length})
            </h3>
            <button
              onClick={() => setShowStaffModal(true)}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90"
            >
              <UserPlus className="h-4 w-4" /> Add Staff
            </button>
          </div>
          {staffLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="h-10 bg-muted animate-pulse rounded-lg" />
              ))}
            </div>
          ) : (staffData?.data ?? []).length === 0 ? (
            <p className="text-sm text-muted-foreground">No staff members at this gym yet. Click "Add Staff" to create one.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Name</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Email</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Role</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Phone</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {(staffData?.data ?? []).map((s: any) => (
                    <tr key={s.id ?? s.userId} className="border-b last:border-0 hover:bg-muted/50">
                      <td className="py-2 px-3 font-medium">{s.firstName ?? s.name ?? '—'} {s.lastName ?? ''}</td>
                      <td className="py-2 px-3 text-muted-foreground">{s.email ?? '—'}</td>
                      <td className="py-2 px-3 capitalize">{(s.staffSubRole ?? s.role ?? '').toLowerCase() || '—'}</td>
                      <td className="py-2 px-3 text-muted-foreground">{s.phone ?? s.phoneNumber ?? '—'}</td>
                      <td className="py-2 px-3">
                        <StatusBadge variant={s.status ?? 'active'} dot />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'Revenue' && <GymRevenuePanel gymId={gymId} />}

      {activeTab === 'Subscription & Features' && (
        <div className="space-y-6">
          {/* Current Plan */}
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold">Current Plan</h3>
              <button
                onClick={openChangePlanModal}
                className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90"
              >
                Change Plan
              </button>
            </div>
            <div className="flex items-center gap-4 mb-4">
              <StatusBadge variant={planNameLower || 'inactive'} label={planLabel} className="text-base px-4 py-1" />
              <span className="text-lg font-bold">{formatCurrency(plan?.monthlyPriceEgp ?? 0)}/mo</span>
            </div>
            {plan && (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div>
                  <p className="text-xs text-muted-foreground mb-1">Members</p>
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                      <div className="h-full bg-emerald-500 rounded-full" style={{ width: `${Math.min((gym.activeMemberCount / Math.max(plan.memberLimit, 1)) * 100, 100)}%` }} />
                    </div>
                    <span className="text-xs font-medium">{gym.activeMemberCount}/{plan.memberLimit}</span>
                  </div>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground mb-1">Trainers</p>
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                      <div className="h-full bg-blue-500 rounded-full" style={{ width: `${Math.min((gym.activeTrainerCount / Math.max(plan.trainerLimit, 1)) * 100, 100)}%` }} />
                    </div>
                    <span className="text-xs font-medium">{gym.activeTrainerCount}/{plan.trainerLimit}</span>
                  </div>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground mb-1">Staff</p>
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                      <div className="h-full bg-amber-500 rounded-full" style={{ width: `${Math.min(((gym as any).activeStaffCount ?? (gym as any).staffCount ?? 0) / Math.max(plan.staffLimit, 1) * 100, 100)}%` }} />
                    </div>
                    <span className="text-xs font-medium">{(gym as any).activeStaffCount ?? (gym as any).staffCount ?? '—'}/{plan.staffLimit}</span>
                  </div>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground mb-1">Plan Tier</p>
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-medium uppercase">{plan.code}</span>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Feature Overrides */}
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold">Feature Overrides</h3>
              <span className="text-xs text-muted-foreground">
                {features.length} features · {gymOverrides.length} override{gymOverrides.length === 1 ? '' : 's'}
              </span>
            </div>
            <div className="overflow-x-auto overflow-y-auto max-h-[32rem]">
              <table className="w-full text-sm">
                <thead className="sticky top-0 bg-card">
                  <tr className="border-b bg-muted/50">
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Feature</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Plan Default</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Override</th>
                    <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {features.map((feature) => {
                    const planDefault = plan ? (featureMatrix[plan.id]?.[feature.id] ?? false) : false;
                    const override = gymOverrides.find((o) => o.featureId === feature.id);
                    return (
                      <tr key={feature.id} className="border-b last:border-0 hover:bg-muted/50">
                        <td className="py-2 px-3 font-medium">{feature.name}</td>
                        <td className="py-2 px-3">
                          <StatusBadge variant={planDefault ? 'active' : 'inactive'} label={planDefault ? 'ON' : 'OFF'} />
                        </td>
                        <td className="py-2 px-3">
                          {override ? (
                            <StatusBadge
                              variant={override.overrideValue === 'force_on' ? 'active' : 'suspended'}
                              label={override.overrideValue === 'force_on' ? 'Force ON' : 'Force OFF'}
                            />
                          ) : (
                            <span className="text-xs text-muted-foreground">Plan Default</span>
                          )}
                        </td>
                        <td className="py-2 px-3">
                          <button
                            onClick={() => handleToggleOverride(feature.id, feature.code, override?.id, planDefault)}
                            disabled={busyOverrideCode === feature.code}
                            className="text-xs text-primary hover:underline disabled:opacity-50"
                          >
                            {busyOverrideCode === feature.code
                              ? '...'
                              : override
                                ? 'Remove Override'
                                : 'Add Override'}
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'Settings' && <GymSettingsPanel gymId={gymId} gymName={gym.name} />}

      {/* Modals */}
      <Modal
        open={showSuspendModal}
        onClose={() => { setShowSuspendModal(false); setSuspendReason(''); }}
        title="Suspend Gym"
        size="md"
        footer={
          <>
            <button
              onClick={() => { setShowSuspendModal(false); setSuspendReason(''); }}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleSuspend}
              disabled={busyAction || !suspendReason.trim()}
              className="rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white hover:bg-amber-700 disabled:opacity-50"
            >
              {busyAction ? 'Suspending...' : 'Suspend'}
            </button>
          </>
        }
      >
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">
            Suspending {gym.name} will restrict access for all staff and members. A reason is required.
          </p>
          <div>
            <label className="block text-sm font-medium mb-1.5">Reason</label>
            <textarea
              value={suspendReason}
              onChange={(e) => setSuspendReason(e.target.value)}
              rows={3}
              maxLength={500}
              placeholder="e.g. Non-payment — 45 days overdue"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
      </Modal>
      <ConfirmModal
        open={showReactivateModal}
        onClose={() => setShowReactivateModal(false)}
        onConfirm={handleReactivate}
        title="Reactivate Gym"
        description={`Restore full access for ${gym.name}? Staff and members will regain access immediately.`}
        confirmLabel="Reactivate"
        variant="success"
        isLoading={busyAction}
      />

      <Modal
        open={showDeleteModal}
        onClose={() => { setShowDeleteModal(false); setDeleteConfirmName(''); }}
        title="Permanently delete gym"
        size="md"
        footer={
          <>
            <button
              onClick={() => { setShowDeleteModal(false); setDeleteConfirmName(''); }}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleDelete}
              disabled={busyAction || deleteConfirmName.trim().toLowerCase() !== (gym.name ?? '').trim().toLowerCase()}
              className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
            >
              {busyAction ? 'Deleting...' : 'Delete forever'}
            </button>
          </>
        }
      >
        <div className="space-y-3">
          <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-950/20 dark:text-red-400">
            <p className="font-semibold">This action is irreversible.</p>
            <p className="mt-1">
              All data for {gym.name} will be permanently destroyed: members, trainers, staff, classes, bookings,
              check-ins, payments, gamification, announcements, and every per-gym schema (g{'{N}'}_*). Identity
              memberships for this gym will also be cleared.
            </p>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">
              Type <span className="font-mono">{gym.name}</span> to confirm
            </label>
            <input
              type="text"
              value={deleteConfirmName}
              onChange={(e) => setDeleteConfirmName(e.target.value)}
              placeholder={gym.name}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
      </Modal>

      <Modal
        open={showEditModal}
        onClose={() => setShowEditModal(false)}
        title="Edit Gym"
        size="md"
        footer={
          <>
            <button
              onClick={() => setShowEditModal(false)}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleSaveEdit}
              disabled={busyAction}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busyAction ? 'Saving...' : 'Save'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          {actionError && (
            <div role="alert" data-testid="gym-edit-error" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
              <span>{actionError}</span>
            </div>
          )}
          <div>
            <label className="block text-sm font-medium mb-1.5">Gym Name</label>
            <input
              type="text"
              value={editForm.name}
              onChange={(e) => setEditForm((f) => ({ ...f, name: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Owner Name</label>
            <input
              type="text"
              value={editForm.ownerName}
              onChange={(e) => setEditForm((f) => ({ ...f, ownerName: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
        </div>
      </Modal>

      <Modal
        open={showChangePlanModal}
        onClose={() => setShowChangePlanModal(false)}
        title="Change Plan"
        size="md"
        footer={
          <>
            <button
              onClick={() => setShowChangePlanModal(false)}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleChangePlan}
              disabled={busyAction || !targetPlanId || !changePlanReason.trim()}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busyAction ? 'Updating...' : 'Change Plan'}
            </button>
          </>
        }
      >
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium mb-1.5">New Plan</label>
            <select
              value={targetPlanId}
              onChange={(e) => setTargetPlanId(e.target.value)}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              {subscriptionPlans.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} — {formatCurrency(p.monthlyPriceEgp)}/mo
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Reason</label>
            <textarea
              value={changePlanReason}
              onChange={(e) => setChangePlanReason(e.target.value)}
              rows={2}
              maxLength={500}
              placeholder="e.g. Upgrade requested by owner"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
          <p className="text-xs text-muted-foreground">
            The gym will be charged on its next billing cycle at the new plan rate.
          </p>
        </div>
      </Modal>

      <Modal
        open={showStaffModal}
        onClose={closeStaffModal}
        title={`Add Staff to ${gym.name}`}
        description="Creates a User (role=GymStaff, activated), enrolls them in this gym, and assigns a StaffRole. The staff member can sign in immediately."
        size="lg"
        footer={
          <>
            <button
              type="button"
              onClick={closeStaffModal}
              disabled={createStaffMutation.isPending}
              className="rounded-lg border px-3 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              form="add-gym-staff-form"
              disabled={createStaffMutation.isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {createStaffMutation.isPending ? 'Creating...' : 'Create Staff'}
            </button>
          </>
        }
      >
        <form id="add-gym-staff-form" onSubmit={handleSubmitStaff} className="space-y-4">
          {staffError && (
            <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {staffError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium">First Name <span className="text-red-500">*</span></label>
              <input
                value={staffForm.firstName}
                onChange={(e) => updateStaffField('firstName', e.target.value)}
                required maxLength={100}
                className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium">Last Name <span className="text-red-500">*</span></label>
              <input
                value={staffForm.lastName}
                onChange={(e) => updateStaffField('lastName', e.target.value)}
                required maxLength={100}
                className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm"
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium">Login Email <span className="text-red-500">*</span></label>
            <input
              type="email"
              value={staffForm.email}
              onChange={(e) => updateStaffField('email', e.target.value)}
              required
              className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium">Password <span className="text-red-500">*</span></label>
            <input
              type="password"
              value={staffForm.password}
              onChange={(e) => updateStaffField('password', e.target.value)}
              required minLength={8}
              className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm"
            />
            <span className="mt-1 block text-xs text-muted-foreground">Min 8 characters. Staff can change after first login.</span>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium">Phone</label>
            <input
              type="tel"
              value={staffForm.phone ?? ''}
              onChange={(e) => updateStaffField('phone', e.target.value)}
              className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm"
            />
            <span className="mt-1 block text-xs text-muted-foreground">Optional. Egyptian local (01XXXXXXXXX) or E.164 (+20...).</span>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium">Staff Role <span className="text-red-500">*</span></label>
            <select
              value={staffForm.staffSubRole}
              onChange={(e) => updateStaffField('staffSubRole', e.target.value as CreateGymStaffPayload['staffSubRole'])}
              className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm"
            >
              <option value="Owner">Owner</option>
              <option value="Manager">Manager</option>
              <option value="Receptionist">Receptionist</option>
            </select>
          </div>
        </form>
      </Modal>
    </div>
  );
}

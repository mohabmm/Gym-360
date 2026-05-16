'use client';

import { useState } from 'react';
import { PageHeader, StatusBadge, Modal, formatDate, extractApiErrors, useInvalidateQueries } from '@gymapp/ui';
import { UserPlus, Check, X, Clock, AlertCircle } from 'lucide-react';
import { usePendingGyms, usePlans, useApproveGym, useRejectGym, useChangePlan } from '@/lib/hooks';
import type { PendingGym } from '@gymapp/ui';

export default function GymOnboardingPage() {
  const [showApproveModal, setShowApproveModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [selectedGym, setSelectedGym] = useState<PendingGym | null>(null);
  const [selectedPlan, setSelectedPlan] = useState('');
  const [rejectReason, setRejectReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);

  const { data: pendingData, refetch: refetchPending } = usePendingGyms();
  const { data: plansData } = usePlans();
  const pendingGyms = pendingData?.data ?? [];
  const subscriptionPlans = plansData?.data ?? [];
  const approveMutation = useApproveGym();
  const rejectMutation = useRejectGym();
  const changePlanMutation = useChangePlan();
  const { invalidate } = useInvalidateQueries();

  const handleConfirmApprove = async () => {
    if (!selectedGym) return;
    setBusy(true);
    setPageError(null);
    try {
      await approveMutation.mutateAsync({ gymId: selectedGym.id });
      if (selectedPlan) {
        // Backend requires a non-empty reason on ChangeGymPlanCommand; supply a default
        // for the onboarding flow since there is no free-text input at this step.
        await changePlanMutation.mutateAsync({
          gymId: selectedGym.id,
          planId: selectedPlan,
          reason: 'Onboarding approval',
        });
      }
      setShowApproveModal(false);
      setSelectedGym(null);
    } catch (err) {
      const apiErrors = extractApiErrors(err);
      const code = apiErrors[0]?.code;
      if (code === 'Gyms.NotPendingApproval') {
        setPageError(`"${selectedGym.name}" is no longer pending — it was already approved or rejected. Refreshing list.`);
        invalidate(['gyms', 'pending']);
        await refetchPending();
        setShowApproveModal(false);
        setSelectedGym(null);
      } else {
        setPageError(apiErrors[0]?.message ?? 'Failed to approve gym.');
      }
    } finally {
      setBusy(false);
    }
  };

  const handleConfirmReject = async () => {
    if (!selectedGym || !rejectReason.trim()) return;
    setBusy(true);
    setPageError(null);
    try {
      await rejectMutation.mutateAsync({ gymId: selectedGym.id, reason: rejectReason });
      setShowRejectModal(false);
      setSelectedGym(null);
      setRejectReason('');
    } catch (err) {
      const apiErrors = extractApiErrors(err);
      const code = apiErrors[0]?.code;
      if (code === 'Gyms.NotPendingApproval') {
        setPageError(`"${selectedGym.name}" is no longer pending — it was already approved or rejected. Refreshing list.`);
        invalidate(['gyms', 'pending']);
        await refetchPending();
        setShowRejectModal(false);
        setSelectedGym(null);
        setRejectReason('');
      } else {
        setPageError(apiErrors[0]?.message ?? 'Failed to reject gym.');
      }
    } finally {
      setBusy(false);
    }
  };

  const handleApprove = (gym: PendingGym) => {
    setSelectedGym(gym);
    if (subscriptionPlans.length > 0 && !selectedPlan) {
      setSelectedPlan(subscriptionPlans[1]?.id ?? subscriptionPlans[0]?.id ?? '');
    }
    setShowApproveModal(true);
  };

  const handleReject = (gym: PendingGym) => {
    setSelectedGym(gym);
    setShowRejectModal(true);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Gym Onboarding"
        description={`${pendingGyms.length} pending approval requests`}
        breadcrumbs={[{ label: 'Gyms', href: '/gyms' }, { label: 'Onboarding' }]}
      />

      {pageError && (
        <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm text-amber-800">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span className="flex-1">{pageError}</span>
          <button onClick={() => setPageError(null)} className="text-amber-500 hover:text-amber-700">✕</button>
        </div>
      )}

      {/* Pending Queue */}
      <div className="space-y-4">
        {pendingGyms.map((gym) => (
          <div key={gym.id} className="rounded-xl border bg-card p-6 shadow-sm hover:shadow-md transition-shadow">
            <div className="flex items-start justify-between">
              <div className="flex items-start gap-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-amber-100 text-amber-600">
                  <UserPlus className="h-6 w-6" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold">{gym.name}</h3>
                  <p className="text-sm text-muted-foreground">
                    {gym.city}, {gym.district} &middot; Requested: {gym.requestedPlan}
                  </p>
                  <div className="mt-2 grid grid-cols-2 gap-x-8 gap-y-1 text-sm">
                    <div>
                      <span className="text-muted-foreground">Owner: </span>
                      <span className="font-medium">{gym.ownerName}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Email: </span>
                      <span className="font-medium">{gym.ownerEmail}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Phone: </span>
                      <span className="font-medium">{gym.ownerPhone}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Submitted: </span>
                      <span className="font-medium">{formatDate(gym.submittedAt)}</span>
                    </div>
                  </div>
                  {gym.notes && (
                    <p className="mt-2 text-sm text-muted-foreground italic">Note: {gym.notes}</p>
                  )}
                </div>
              </div>
              <div className="flex items-center gap-2">
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground mr-4">
                  <Clock className="h-3.5 w-3.5" />
                  <span>{formatDate(gym.submittedAt)}</span>
                </div>
                <button
                  onClick={() => handleReject(gym)}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100"
                >
                  <X className="h-4 w-4" />
                  Reject
                </button>
                <button
                  onClick={() => handleApprove(gym)}
                  className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-700"
                >
                  <Check className="h-4 w-4" />
                  Approve
                </button>
              </div>
            </div>
          </div>
        ))}

        {pendingGyms.length === 0 && (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 mb-4">
              <Check className="h-8 w-8 text-emerald-600" />
            </div>
            <h3 className="text-lg font-semibold">All caught up!</h3>
            <p className="text-sm text-muted-foreground">No pending gym applications.</p>
          </div>
        )}
      </div>

      {/* Approve Modal */}
      <Modal
        open={showApproveModal}
        onClose={() => setShowApproveModal(false)}
        title={`Approve ${selectedGym?.name}`}
        size="md"
        footer={
          <>
            <button
              onClick={() => setShowApproveModal(false)}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleConfirmApprove}
              disabled={busy}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              {busy ? 'Approving...' : 'Approve & Create'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Assign Plan</label>
            <select
              value={selectedPlan}
              onChange={(e) => setSelectedPlan(e.target.value)}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              {subscriptionPlans.map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {plan.name} - {plan.monthlyPriceEgp === 0 ? 'Free' : `EGP ${plan.monthlyPriceEgp}/mo`}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Gym Code</label>
            <input
              type="text"
              defaultValue={selectedGym?.name.replace(/\s+/g, '').substring(0, 6).toUpperCase()}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div className="flex items-center gap-2">
            <input type="checkbox" defaultChecked id="send-welcome" className="rounded border-input" />
            <label htmlFor="send-welcome" className="text-sm">Send welcome email to owner</label>
          </div>
        </div>
      </Modal>

      {/* Reject Modal */}
      <Modal
        open={showRejectModal}
        onClose={() => setShowRejectModal(false)}
        title={`Reject ${selectedGym?.name}`}
        size="md"
        footer={
          <>
            <button
              onClick={() => setShowRejectModal(false)}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleConfirmReject}
              disabled={busy || !rejectReason.trim()}
              className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
            >
              {busy ? 'Rejecting...' : 'Reject Application'}
            </button>
          </>
        }
      >
        <div>
          <label className="block text-sm font-medium mb-1.5">Reason for rejection</label>
          <textarea
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            placeholder="Explain why this application is being rejected..."
            rows={4}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm resize-none"
          />
        </div>
      </Modal>
    </div>
  );
}

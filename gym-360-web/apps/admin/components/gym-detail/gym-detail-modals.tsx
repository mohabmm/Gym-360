'use client';

import { useState, type ReactNode } from 'react';
import { ConfirmModal, Modal, formatCurrency, type Gym, type SubscriptionPlan } from '@gym360/ui';
import { AlertCircle } from 'lucide-react';
import { confirmsGymName } from '@/lib/gym-detail';
import type { GymDetailActions } from './use-gym-detail-actions';

const CANCEL_CLASS = 'rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted';
const PRIMARY_CLASS = 'rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50';

interface ModalFooterProps {
  onCancel: () => void;
  onConfirm: () => void;
  disabled: boolean;
  busy: boolean;
  busyLabel: string;
  label: string;
  confirmClass: string;
}

function ModalFooter({ onCancel, onConfirm, disabled, busy, busyLabel, label, confirmClass }: ModalFooterProps) {
  return (
    <>
      <button onClick={onCancel} className={CANCEL_CLASS}>
        Cancel
      </button>
      <button onClick={onConfirm} disabled={disabled} className={confirmClass}>
        {busy ? busyLabel : label}
      </button>
    </>
  );
}

function Field({ label, children }: { label: ReactNode; children: ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      {children}
    </div>
  );
}

interface GymModalProps {
  gym: Gym;
  actions: GymDetailActions;
  onClose: () => void;
}

export function SuspendGymModal({ gym, actions, onClose }: GymModalProps) {
  const [reason, setReason] = useState('');
  const handleSuspend = async () => {
    if (!reason.trim()) return;
    await actions.suspend(reason.trim(), onClose);
  };

  return (
    <Modal
      open
      onClose={onClose}
      title="Suspend Gym"
      size="md"
      footer={
        <ModalFooter
          onCancel={onClose}
          onConfirm={handleSuspend}
          disabled={actions.busyAction || !reason.trim()}
          busy={actions.busyAction}
          busyLabel="Suspending..."
          label="Suspend"
          confirmClass="rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white hover:bg-amber-700 disabled:opacity-50"
        />
      }
    >
      <div className="space-y-3">
        <p className="text-sm text-muted-foreground">
          Suspending {gym.name} will restrict access for all staff and members. A reason is required.
        </p>
        <Field label="Reason">
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={3}
            maxLength={500}
            placeholder="e.g. Non-payment — 45 days overdue"
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
          />
        </Field>
      </div>
    </Modal>
  );
}

export function ReactivateGymModal({ gym, actions, onClose }: GymModalProps) {
  return (
    <ConfirmModal
      open
      onClose={onClose}
      onConfirm={() => actions.reactivate(onClose)}
      title="Reactivate Gym"
      description={`Restore full access for ${gym.name}? Staff and members will regain access immediately.`}
      confirmLabel="Reactivate"
      variant="success"
      isLoading={actions.busyAction}
    />
  );
}

export function DeleteGymModal({ gym, actions, onClose }: GymModalProps) {
  const [confirmName, setConfirmName] = useState('');
  const nameMatches = confirmsGymName(confirmName, gym.name);
  const handleDelete = async () => {
    if (!nameMatches) return;
    await actions.remove(onClose);
  };

  return (
    <Modal
      open
      onClose={onClose}
      title="Permanently delete gym"
      size="md"
      footer={
        <ModalFooter
          onCancel={onClose}
          onConfirm={handleDelete}
          disabled={actions.busyAction || !nameMatches}
          busy={actions.busyAction}
          busyLabel="Deleting..."
          label="Delete forever"
          confirmClass="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
        />
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
        <Field label={<>Type <span className="font-mono">{gym.name}</span> to confirm</>}>
          <input
            type="text"
            value={confirmName}
            onChange={(e) => setConfirmName(e.target.value)}
            placeholder={gym.name}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
          />
        </Field>
      </div>
    </Modal>
  );
}

export function EditGymModal({ gym, actions, onClose }: GymModalProps) {
  const [form, setForm] = useState({ name: gym.name, ownerName: gym.ownerName ?? '' });

  return (
    <Modal
      open
      onClose={onClose}
      title="Edit Gym"
      size="md"
      footer={
        <ModalFooter
          onCancel={onClose}
          onConfirm={() => actions.saveEdit(form, onClose)}
          disabled={actions.busyAction}
          busy={actions.busyAction}
          busyLabel="Saving..."
          label="Save"
          confirmClass={PRIMARY_CLASS}
        />
      }
    >
      <div className="space-y-4">
        {actions.actionError && (
          <div role="alert" data-testid="gym-edit-error" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
            <span>{actions.actionError}</span>
          </div>
        )}
        <Field label="Gym Name">
          <input
            type="text"
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
            className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
          />
        </Field>
        <Field label="Owner Name">
          <input
            type="text"
            value={form.ownerName}
            onChange={(e) => setForm((f) => ({ ...f, ownerName: e.target.value }))}
            className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
          />
        </Field>
      </div>
    </Modal>
  );
}

interface ChangePlanModalProps {
  actions: GymDetailActions;
  plans: SubscriptionPlan[];
  currentPlanId: string;
  onClose: () => void;
}

export function ChangePlanModal({ actions, plans, currentPlanId, onClose }: ChangePlanModalProps) {
  const [targetPlanId, setTargetPlanId] = useState(currentPlanId);
  const [reason, setReason] = useState('');
  const canSubmit = Boolean(targetPlanId) && Boolean(reason.trim());
  const handleChangePlan = async () => {
    if (!canSubmit) return;
    await actions.changePlan({ planId: targetPlanId, reason: reason.trim() }, onClose);
  };

  return (
    <Modal
      open
      onClose={onClose}
      title="Change Plan"
      size="md"
      footer={
        <ModalFooter
          onCancel={onClose}
          onConfirm={handleChangePlan}
          disabled={actions.busyAction || !canSubmit}
          busy={actions.busyAction}
          busyLabel="Updating..."
          label="Change Plan"
          confirmClass={PRIMARY_CLASS}
        />
      }
    >
      <div className="space-y-3">
        <Field label="New Plan">
          <select
            value={targetPlanId}
            onChange={(e) => setTargetPlanId(e.target.value)}
            className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
          >
            {plans.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name} — {formatCurrency(p.monthlyPriceEgp)}/mo
              </option>
            ))}
          </select>
        </Field>
        <Field label="Reason">
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={2}
            maxLength={500}
            placeholder="e.g. Upgrade requested by owner"
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
          />
        </Field>
        <p className="text-xs text-muted-foreground">
          The gym will be charged on its next billing cycle at the new plan rate.
        </p>
      </div>
    </Modal>
  );
}

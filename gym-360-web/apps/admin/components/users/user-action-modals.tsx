'use client';

import { useState } from 'react';
import { AlertCircle } from 'lucide-react';
import { Modal } from '@gym360/ui';
import type { AdminUser } from '@gym360/ui';
import type { UserActions } from './use-user-actions';

const CANCEL_CLASS = 'rounded-lg border border-input bg-background px-3 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50';
const SUSPEND_CLASS = 'rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white hover:bg-amber-700 disabled:opacity-50';
const DELETE_CLASS = 'rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50';
const INPUT_CLASS = 'h-9 w-full rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-red-300';

type FooterProps = { actions: UserActions; label: string; busyLabel: string; className: string; disabled?: boolean };

function Footer({ actions, label, busyLabel, className, disabled }: FooterProps) {
  const busy = actions.busyUserId !== null;
  return (
    <>
      <button type="button" onClick={actions.cancel} disabled={busy} className={CANCEL_CLASS}>Cancel</button>
      <button type="button" onClick={actions.confirm} disabled={busy || disabled} className={className}>
        {busy ? busyLabel : label}
      </button>
    </>
  );
}

function ErrorBanner({ message }: { message: string | null }) {
  if (!message) return null;
  return (
    <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
      <AlertCircle className="h-4 w-4 shrink-0" />
      {message}
    </div>
  );
}

function SuspendUserModal({ user, actions }: { user: AdminUser; actions: UserActions }) {
  return (
    <Modal
      open
      onClose={actions.cancel}
      title={`Suspend ${user.firstName} ${user.lastName}?`}
      description="They are signed out right away on every device and cannot sign in until you reactivate them. Their data is kept."
      footer={<Footer actions={actions} label="Suspend" busyLabel="Suspending..." className={SUSPEND_CLASS} />}
    >
      <ErrorBanner message={actions.error} />
    </Modal>
  );
}

function DeleteUserModal({ user, actions }: { user: AdminUser; actions: UserActions }) {
  const [typed, setTyped] = useState('');
  const matches = typed.trim().toLowerCase() === user.email.toLowerCase();
  return (
    <Modal
      open
      onClose={actions.cancel}
      title={`Permanently delete ${user.firstName} ${user.lastName}?`}
      description="This cannot be undone. The account and all personal data (progress, workouts, nutrition, sessions, chats, notifications) are erased in every gym. Payment and trainer-earnings records are kept without personal details, as required for financial records."
      footer={<Footer actions={actions} label="Delete forever" busyLabel="Deleting..." className={DELETE_CLASS} disabled={!matches} />}
    >
      <div className="space-y-3">
        <ErrorBanner message={actions.error} />
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-foreground">
            Type <span className="font-mono">{user.email}</span> to confirm
          </span>
          <input value={typed} onChange={(e) => setTyped(e.target.value)} autoComplete="off" className={INPUT_CLASS} />
        </label>
      </div>
    </Modal>
  );
}

/** Confirm dialogs for the pending suspend / delete action, if any. */
export function UserActionModals({ actions }: { actions: UserActions }) {
  const { pending } = actions;
  if (!pending) return null;
  return pending.kind === 'suspend'
    ? <SuspendUserModal user={pending.user} actions={actions} />
    : <DeleteUserModal key={pending.user.id} user={pending.user} actions={actions} />;
}

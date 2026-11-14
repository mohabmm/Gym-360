'use client';

import { Pause, Play, Trash2 } from 'lucide-react';
import type { AdminUser } from '@gym360/ui';
import type { UserActions } from './use-user-actions';

const BUTTON = 'inline-flex items-center gap-1 rounded-lg border px-2 py-1 text-xs font-medium disabled:opacity-50';
const SUSPEND_CLASS = `${BUTTON} border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100`;
const ACTIVATE_CLASS = `${BUTTON} border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100`;
const DELETE_CLASS = `${BUTTON} border-red-200 bg-red-50 text-red-700 hover:bg-red-100`;

function StatusButton({ user, actions, busy }: { user: AdminUser; actions: UserActions; busy: boolean }) {
  const name = `${user.firstName} ${user.lastName}`;
  if (user.status === 'suspended') {
    return (
      <button onClick={() => actions.activate(user)} disabled={busy} aria-label={`Activate ${name}`} className={ACTIVATE_CLASS}>
        <Play className="h-3 w-3" />
        {busy ? '...' : 'Activate'}
      </button>
    );
  }
  if (user.status === 'active') {
    return (
      <button onClick={() => actions.requestSuspend(user)} disabled={busy} aria-label={`Suspend ${name}`} className={SUSPEND_CLASS}>
        <Pause className="h-3 w-3" />
        Suspend
      </button>
    );
  }
  return null;
}

/** Row actions for the Users table. Hidden on the signed-in admin's own row. */
export function UserRowActions({ user, actions }: { user: AdminUser; actions: UserActions }) {
  if (user.isYou) return null;
  const busy = actions.busyUserId === user.id;
  return (
    <div className="flex items-center justify-end gap-2">
      <StatusButton user={user} actions={actions} busy={busy} />
      <button
        onClick={() => actions.requestDelete(user)}
        disabled={busy}
        aria-label={`Delete ${user.firstName} ${user.lastName}`}
        className={DELETE_CLASS}
      >
        <Trash2 className="h-3 w-3" />
        Delete
      </button>
    </div>
  );
}

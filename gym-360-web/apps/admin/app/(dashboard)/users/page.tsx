'use client';

import { useState, useMemo } from 'react';
import { PageHeader, DataTable } from '@gym360/ui';
import { Filter, AlertCircle, UserPlus } from 'lucide-react';
import { useAdminUsers } from '@/lib/hooks';
import { useUserActions } from '@/components/users/use-user-actions';
import { UserActionModals } from '@/components/users/user-action-modals';
import { AddGymOwnerModal } from '@/components/users/add-gym-owner-modal';
import { buildUserColumns } from '@/components/users/user-columns';
import { TableSkeleton } from '@gym360/ui';
import type { AdminUser } from '@gym360/ui';

export default function UsersPage() {
  const [roleFilter, setRoleFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const userActions = useUserActions();

  const [showOwnerModal, setShowOwnerModal] = useState(false);
  const columns = buildUserColumns(userActions);

  const { data: usersData, isLoading, isError } = useAdminUsers();
  const allUsers = usersData?.data ?? [];

  const filteredUsers = useMemo<AdminUser[]>(
    () => allUsers.filter((user) => matchesFilter(roleFilter, user.role) && matchesFilter(statusFilter, user.status)),
    [allUsers, roleFilter, statusFilter],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title="Users"
        description={isLoading ? 'Loading...' : `${allUsers.length} users across all roles`}
        actions={
          <button
            onClick={() => setShowOwnerModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            <UserPlus className="h-4 w-4" />
            Add Gym Owner
          </button>
        }
      />

      {!userActions.pending && <ActionErrorBanner message={userActions.error} onDismiss={userActions.clearError} />}

      <UserActionModals actions={userActions} />

      <div className="flex items-center gap-3">
        <Filter className="h-4 w-4 text-muted-foreground" />
        <select
          value={roleFilter}
          onChange={(e) => setRoleFilter(e.target.value)}
          className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
        >
          <option value="all">All Roles</option>
          <option value="platform_admin">Platform Admin</option>
          <option value="gym_owner">Gym Owner</option>
          <option value="gym_staff">Gym Staff</option>
          <option value="trainer">Trainer</option>
          <option value="trainee">Trainee</option>
        </select>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
        >
          <option value="all">All Statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
          <option value="suspended">Suspended</option>
          <option value="pending">Pending</option>
        </select>
      </div>

      <UsersTable isError={isError} isLoading={isLoading}>
        <DataTable
          data={filteredUsers}
          columns={columns}
          searchPlaceholder="Search users by name or email..."
          searchColumn="email"
          enableExport
        />
      </UsersTable>

      <AddGymOwnerModal open={showOwnerModal} onClose={() => setShowOwnerModal(false)} />
    </div>
  );
}

const matchesFilter = (filter: string, value: string) => filter === 'all' || value === filter;

function ActionErrorBanner({ message, onDismiss }: { message: string | null; onDismiss: () => void }) {
  if (!message) return null;
  return (
    <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
      <AlertCircle className="h-4 w-4 shrink-0" />
      {message}
      <button onClick={onDismiss} className="ml-auto text-red-400 hover:text-red-600">✕</button>
    </div>
  );
}

function UsersTable({ isError, isLoading, children }: { isError: boolean; isLoading: boolean; children: React.ReactNode }) {
  if (isError) {
    return (
      <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
        <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
        <p className="text-sm text-red-700 dark:text-red-400">Failed to load users. Please refresh the page.</p>
      </div>
    );
  }
  if (isLoading) return <TableSkeleton rows={8} columns={6} />;
  return <>{children}</>;
}

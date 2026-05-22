'use client';

import { useState, useMemo } from 'react';
import { PageHeader, DataTable, StatusBadge, formatDate, apiPatch, IDENTITY, useInvalidateQueries, Modal, extractApiErrors } from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Filter, Pause, Play, AlertCircle, UserPlus } from 'lucide-react';
import { useAdminUsers, useCreateGymOwner, type CreateGymOwnerPayload } from '@/lib/hooks';
import { TableSkeleton } from '@gym360/ui';
import type { AdminUser } from '@gym360/ui';

const EMPTY_OWNER_FORM: CreateGymOwnerPayload = {
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phone: '',
  companyName: '',
  billingEmail: '',
  planTier: 'Pro',
  dashboardMode: 'Both',
};

export default function UsersPage() {
  const [roleFilter, setRoleFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [actionError, setActionError] = useState<string | null>(null);
  const [busyUserId, setBusyUserId] = useState<string | null>(null);
  const { invalidate } = useInvalidateQueries();

  const [showOwnerModal, setShowOwnerModal] = useState(false);
  const [ownerForm, setOwnerForm] = useState<CreateGymOwnerPayload>(EMPTY_OWNER_FORM);
  const [ownerError, setOwnerError] = useState<string | null>(null);
  const createOwnerMutation = useCreateGymOwner();

  const { data: usersData, isLoading, isError } = useAdminUsers();
  const allUsers = usersData?.data ?? [];

  const updateOwnerField = <K extends keyof CreateGymOwnerPayload>(key: K, value: CreateGymOwnerPayload[K]) => {
    setOwnerForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmitOwner = async (e: React.FormEvent) => {
    e.preventDefault();
    setOwnerError(null);
    try {
      await createOwnerMutation.mutateAsync({
        ...ownerForm,
        phone: ownerForm.phone?.trim() ? ownerForm.phone.trim() : undefined,
      });
      invalidate(['admin-users']);
      setShowOwnerModal(false);
      setOwnerForm(EMPTY_OWNER_FORM);
    } catch (err) {
      const apiErrors = extractApiErrors(err);
      setOwnerError(apiErrors[0]?.message ?? 'Failed to create gym owner.');
    }
  };

  const handleCloseOwnerModal = () => {
    if (createOwnerMutation.isPending) return;
    setShowOwnerModal(false);
    setOwnerError(null);
    setOwnerForm(EMPTY_OWNER_FORM);
  };

  const handleToggleStatus = async (user: AdminUser) => {
    const nextStatus = user.status === 'active' ? 'suspended' : 'active';
    setBusyUserId(user.id);
    setActionError(null);
    try {
      await apiPatch(IDENTITY.ADMIN_USER(user.id), { status: nextStatus });
      invalidate(['admin-users']);
    } catch {
      setActionError(`Failed to update ${user.firstName} ${user.lastName}`);
    } finally {
      setBusyUserId(null);
    }
  };

  const columns: ColumnDef<AdminUser, unknown>[] = [
    {
      id: 'name',
      header: 'Name',
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-bold text-primary">
            {row.original.firstName[0]}{row.original.lastName[0]}
          </div>
          <div>
            <p className="font-medium">{row.original.firstName} {row.original.lastName}</p>
            <p className="text-xs text-muted-foreground">{row.original.firstNameAr} {row.original.lastNameAr}</p>
          </div>
        </div>
      ),
    },
    { accessorKey: 'email', header: 'Email' },
    {
      accessorKey: 'role',
      header: 'Role',
      cell: ({ row }) => (
        <div className="flex items-center gap-1.5">
          <StatusBadge variant={row.original.role === 'platform_admin' ? (row.original.adminRole ?? 'default') : row.original.role} />
        </div>
      ),
    },
    {
      id: 'gyms',
      header: 'Gym',
      cell: ({ row }) => {
        const gyms = row.original.gyms ?? [];
        if (gyms.length === 0) {
          return <span className="text-xs text-muted-foreground">—</span>;
        }
        return (
          <div className="flex flex-wrap gap-1">
            {gyms.map((g) => (
              <span
                key={g.id}
                className="inline-flex items-center rounded-md border border-border bg-muted px-2 py-0.5 text-xs"
                title={g.name}
              >
                {g.name || g.id.slice(0, 8)}
              </span>
            ))}
          </div>
        );
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge variant={row.original.status} dot />,
    },
    {
      accessorKey: 'createdAt',
      header: 'Joined',
      cell: ({ row }) => formatDate(row.original.createdAt),
    },
    {
      accessorKey: 'lastLoginAt',
      header: 'Last Login',
      cell: ({ row }) => row.original.lastLoginAt ? formatDate(row.original.lastLoginAt) : 'Never',
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const user = row.original;
        const isBusy = busyUserId === user.id;
        if (user.status === 'active') {
          return (
            <button
              onClick={() => handleToggleStatus(user)}
              disabled={isBusy}
              aria-label={`Suspend ${user.firstName} ${user.lastName}`}
              className="inline-flex items-center gap-1 rounded-lg border border-amber-200 bg-amber-50 px-2 py-1 text-xs font-medium text-amber-700 hover:bg-amber-100 disabled:opacity-50"
            >
              <Pause className="h-3 w-3" />
              {isBusy ? '...' : 'Suspend'}
            </button>
          );
        }
        if (user.status === 'suspended') {
          return (
            <button
              onClick={() => handleToggleStatus(user)}
              disabled={isBusy}
              aria-label={`Activate ${user.firstName} ${user.lastName}`}
              className="inline-flex items-center gap-1 rounded-lg border border-emerald-200 bg-emerald-50 px-2 py-1 text-xs font-medium text-emerald-700 hover:bg-emerald-100 disabled:opacity-50"
            >
              <Play className="h-3 w-3" />
              {isBusy ? '...' : 'Activate'}
            </button>
          );
        }
        return null;
      },
    },
  ];

  const filteredUsers = useMemo<AdminUser[]>(() => {
    return allUsers.filter((user) => {
      if (roleFilter !== 'all' && user.role !== roleFilter) return false;
      if (statusFilter !== 'all' && user.status !== statusFilter) return false;
      return true;
    });
  }, [allUsers, roleFilter, statusFilter]);

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

      {actionError && (
        <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {actionError}
          <button onClick={() => setActionError(null)} className="ml-auto text-red-400 hover:text-red-600">✕</button>
        </div>
      )}

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

      {isError ? (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">Failed to load users. Please refresh the page.</p>
        </div>
      ) : isLoading ? (
        <TableSkeleton rows={8} columns={6} />
      ) : (
        <DataTable
          data={filteredUsers}
          columns={columns}
          searchPlaceholder="Search users by name or email..."
          searchColumn="email"
          enableExport
        />
      )}

      <Modal
        open={showOwnerModal}
        onClose={handleCloseOwnerModal}
        title="Add Gym Owner"
        description="Creates a User (role=GymOwner, activated), an Owner record, and a Contract row. The owner can sign in immediately. Gyms are added afterwards from the Gyms page."
        size="lg"
        footer={
          <>
            <button
              type="button"
              onClick={handleCloseOwnerModal}
              disabled={createOwnerMutation.isPending}
              className="rounded-lg border border-input bg-background px-3 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              form="create-owner-form"
              disabled={createOwnerMutation.isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              {createOwnerMutation.isPending ? 'Creating...' : 'Create Owner'}
            </button>
          </>
        }
      >
        <form id="create-owner-form" onSubmit={handleSubmitOwner} className="space-y-4">
          {ownerError && (
            <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {ownerError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <Field label="First Name" required>
              <input
                value={ownerForm.firstName}
                onChange={(e) => updateOwnerField('firstName', e.target.value)}
                required
                maxLength={100}
                className={inputClass}
              />
            </Field>
            <Field label="Last Name" required>
              <input
                value={ownerForm.lastName}
                onChange={(e) => updateOwnerField('lastName', e.target.value)}
                required
                maxLength={100}
                className={inputClass}
              />
            </Field>
          </div>

          <Field label="Login Email" required>
            <input
              type="email"
              value={ownerForm.email}
              onChange={(e) => updateOwnerField('email', e.target.value)}
              required
              className={inputClass}
            />
          </Field>

          <Field label="Password" required hint="Min 8 characters. Owner can change after first login.">
            <input
              type="password"
              value={ownerForm.password}
              onChange={(e) => updateOwnerField('password', e.target.value)}
              required
              minLength={8}
              className={inputClass}
            />
          </Field>

          <Field label="Phone" hint="Optional. Egyptian local (01XXXXXXXXX) or E.164 (+20...).">
            <input
              type="tel"
              value={ownerForm.phone ?? ''}
              onChange={(e) => updateOwnerField('phone', e.target.value)}
              className={inputClass}
            />
          </Field>

          <hr className="border-border" />

          <Field label="Company / Brand Name" required>
            <input
              value={ownerForm.companyName}
              onChange={(e) => updateOwnerField('companyName', e.target.value)}
              required
              maxLength={200}
              className={inputClass}
            />
          </Field>

          <Field label="Billing Email" required>
            <input
              type="email"
              value={ownerForm.billingEmail}
              onChange={(e) => updateOwnerField('billingEmail', e.target.value)}
              required
              className={inputClass}
            />
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="Plan Tier" required>
              <select
                value={ownerForm.planTier}
                onChange={(e) => updateOwnerField('planTier', e.target.value as CreateGymOwnerPayload['planTier'])}
                className={inputClass}
              >
                <option value="Free">Free</option>
                <option value="Starter">Starter</option>
                <option value="Pro">Pro</option>
                <option value="Business">Business</option>
              </select>
            </Field>
            <Field label="Dashboard Mode" required>
              <select
                value={ownerForm.dashboardMode}
                onChange={(e) => updateOwnerField('dashboardMode', e.target.value as CreateGymOwnerPayload['dashboardMode'])}
                className={inputClass}
              >
                <option value="Unified">Unified</option>
                <option value="PerGym">Per-Gym</option>
                <option value="Both">Both</option>
              </select>
            </Field>
          </div>
        </form>
      </Modal>
    </div>
  );
}

const inputClass = 'h-9 w-full rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30';

function Field({
  label,
  required,
  hint,
  children,
}: {
  label: string;
  required?: boolean;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-foreground">
        {label} {required && <span className="text-red-500">*</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted-foreground">{hint}</span>}
    </label>
  );
}

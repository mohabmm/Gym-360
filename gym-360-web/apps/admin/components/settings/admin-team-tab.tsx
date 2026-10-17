'use client';

import { StatusBadge, DataTable, Modal, type AdminUser } from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Plus, Trash2 } from 'lucide-react';
import { LabeledField } from './settings-tabs';
import { isAdminFormComplete, type AdminRole, type AddAdminForm, type SettingsActions } from './use-settings-actions';

const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

function adminColumns(onRemove: (admin: AdminUser) => void): ColumnDef<AdminUser, unknown>[] {
  return [
    {
      accessorKey: 'firstName',
      header: 'Name',
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-bold text-primary">
            {row.original.firstName[0]}{row.original.lastName[0]}
          </div>
          <div>
            <p className="font-medium">{row.original.firstName} {row.original.lastName}</p>
            <p className="text-xs text-muted-foreground">{row.original.email}</p>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'adminRole',
      header: 'Role',
      cell: ({ row }) =>
        row.original.adminRole ? (
          <StatusBadge variant={row.original.adminRole} />
        ) : (
          <span className="text-xs text-muted-foreground">—</span>
        ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge variant={row.original.status} dot />,
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className="flex items-center gap-1">
          {!row.original.isYou && (
            <button
              onClick={() => onRemove(row.original)}
              aria-label={`Remove ${row.original.firstName} ${row.original.lastName}`}
              className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground hover:text-red-500"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      ),
    },
  ];
}

function AddAdminModal({ actions }: { actions: SettingsActions }) {
  const { busy, showAddAdmin, setShowAddAdmin, addAdminForm: form, setAddAdminForm, addAdmin } = actions;
  const set = <K extends keyof AddAdminForm>(key: K) => (value: AddAdminForm[K]) =>
    setAddAdminForm((f) => ({ ...f, [key]: value }));

  return (
    <Modal
      open={showAddAdmin}
      onClose={() => setShowAddAdmin(false)}
      title="Add Admin User"
      size="md"
      footer={
        <>
          <button onClick={() => setShowAddAdmin(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
          <button
            onClick={addAdmin}
            disabled={busy || !isAdminFormComplete(form)}
            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          >
            {busy ? 'Adding...' : 'Add Admin'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <LabeledField label="First Name">
            <input type="text" value={form.firstName} onChange={(e) => set('firstName')(e.target.value)} className={INPUT_CLASS} />
          </LabeledField>
          <LabeledField label="Last Name">
            <input type="text" value={form.lastName} onChange={(e) => set('lastName')(e.target.value)} className={INPUT_CLASS} />
          </LabeledField>
        </div>
        <LabeledField label="Email">
          <input type="email" value={form.email} onChange={(e) => set('email')(e.target.value)} className={INPUT_CLASS} />
        </LabeledField>
        <LabeledField label="Temporary Password (optional)">
          <input
            type="text"
            value={form.password}
            placeholder="Leave blank for a generated password"
            onChange={(e) => set('password')(e.target.value)}
            className={INPUT_CLASS}
          />
        </LabeledField>
        <LabeledField label="Role">
          <select value={form.adminRole} onChange={(e) => set('adminRole')(e.target.value as AdminRole)} className={INPUT_CLASS}>
            <option value="super_admin">Super Admin</option>
            <option value="admin">Admin</option>
            <option value="support">Support</option>
            <option value="analyst">Analyst</option>
          </select>
        </LabeledField>
      </div>
    </Modal>
  );
}

export function AdminTeamTab({ adminUsers, actions }: { adminUsers: AdminUser[]; actions: SettingsActions }) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          Manage platform administrators. Only super_admin can access this section.
        </p>
        <button
          onClick={() => actions.setShowAddAdmin(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
        >
          <Plus className="h-4 w-4" /> Add Admin
        </button>
      </div>
      <DataTable data={adminUsers} columns={adminColumns(actions.setDeleteAdmin)} />
      <AddAdminModal actions={actions} />
    </div>
  );
}

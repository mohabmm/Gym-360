'use client';

import { StatusBadge, formatDate } from '@gym360/ui';
import type { AdminUser } from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { UserRowActions } from './user-row-actions';
import type { UserActions } from './use-user-actions';

function UserGymsCell({ gyms }: { gyms: NonNullable<AdminUser['gyms']> }) {
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
}

/** Column definitions for the Super Admin Users table. */
export function buildUserColumns(actions: UserActions): ColumnDef<AdminUser, unknown>[] {
  return [
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
      cell: ({ row }) => <UserGymsCell gyms={row.original.gyms ?? []} />,
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
      cell: ({ row }) => <UserRowActions user={row.original} actions={actions} />,
    },
  ];
}

'use client';

import type { ReactNode } from 'react';
import { StatusBadge } from '@gym360/ui';
import { Shield, UserPlus } from 'lucide-react';
import { useGymMembers, useGymTrainers, useGymStaff } from '@/lib/hooks';

const TH_LEFT = 'text-left py-2 px-3 font-semibold text-muted-foreground';
const TH_RIGHT = 'text-right py-2 px-3 font-semibold text-muted-foreground';
const ROW = 'border-b last:border-0 hover:bg-muted/50';

function RowsSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="h-10 bg-muted animate-pulse rounded-lg" />
      ))}
    </div>
  );
}

interface PeopleTableProps {
  isLoading: boolean;
  isEmpty: boolean;
  emptyText: string;
  head: ReactNode;
  children: ReactNode;
}

/** Loading skeleton → empty text → table, shared by the Members / Trainers / Staff tabs. */
function PeopleTable({ isLoading, isEmpty, emptyText, head, children }: PeopleTableProps) {
  if (isLoading) return <RowsSkeleton />;
  if (isEmpty) return <p className="text-sm text-muted-foreground">{emptyText}</p>;
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50">{head}</tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  );
}

function ActivePill({ status }: { status: string }) {
  const tone = status === 'active' ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600';
  return <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${tone}`}>{status}</span>;
}

function formatJoinDate(joinDate: string | undefined): string {
  return joinDate ? new Date(joinDate).toLocaleDateString() : '—';
}

interface PeopleColumn<T> {
  header: string;
  align?: 'right';
  cellClass: string;
  render: (row: T) => ReactNode;
}

interface PeopleTabConfig<T extends { id: string }> {
  label: string;
  emptyText: string;
  columns: PeopleColumn<T>[];
}

interface PeopleTabCardProps<T extends { id: string }> extends PeopleTabConfig<T> {
  count: number;
  query: { data?: { data?: T[] }; isLoading: boolean };
}

/** Card + table body for the Members / Trainers tabs, driven by a column config. */
function PeopleTabCard<T extends { id: string }>({ label, count, query, emptyText, columns }: PeopleTabCardProps<T>) {
  const rows = query.data?.data ?? [];
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">{label} ({count})</h3>
      <PeopleTable
        isLoading={query.isLoading}
        isEmpty={rows.length === 0}
        emptyText={emptyText}
        head={columns.map((c) => (
          <th key={c.header} className={c.align === 'right' ? TH_RIGHT : TH_LEFT}>{c.header}</th>
        ))}
      >
        {rows.map((row) => (
          <tr key={row.id} className={ROW}>
            {columns.map((c) => (
              <td key={c.header} className={c.cellClass}>{c.render(row)}</td>
            ))}
          </tr>
        ))}
      </PeopleTable>
    </div>
  );
}

type GymMemberRow = NonNullable<ReturnType<typeof useGymMembers>['data']>['data'][number];
type GymTrainerRow = NonNullable<ReturnType<typeof useGymTrainers>['data']>['data'][number];

const MEMBERS_TAB: PeopleTabConfig<GymMemberRow> = {
  label: 'Members',
  emptyText: 'No members found for this gym.',
  columns: [
    { header: 'Name', cellClass: 'py-2 px-3 font-medium', render: (m) => m.name },
    { header: 'Email', cellClass: 'py-2 px-3 text-muted-foreground', render: (m) => m.email },
    { header: 'Membership', cellClass: 'py-2 px-3 capitalize', render: (m) => m.membershipType?.replace('_', ' ') },
    { header: 'Status', cellClass: 'py-2 px-3', render: (m) => <ActivePill status={m.status} /> },
    { header: 'Joined', cellClass: 'py-2 px-3 text-muted-foreground', render: (m) => formatJoinDate(m.joinDate) },
  ],
};

const TRAINERS_TAB: PeopleTabConfig<GymTrainerRow> = {
  label: 'Trainers',
  emptyText: 'No trainers found for this gym.',
  columns: [
    { header: 'Name', cellClass: 'py-2 px-3 font-medium', render: (t) => t.name },
    { header: 'Specializations', cellClass: 'py-2 px-3 text-muted-foreground', render: (t) => (t.specializations ?? []).join(', ') || '—' },
    { header: 'Active Clients', align: 'right', cellClass: 'py-2 px-3 text-right', render: (t) => t.activeMemberCount },
    { header: 'Rating', align: 'right', cellClass: 'py-2 px-3 text-right', render: (t) => (t.rating ? t.rating.toFixed(1) : '—') },
    { header: 'Status', cellClass: 'py-2 px-3', render: (t) => <ActivePill status={t.status} /> },
  ],
};

export function GymMembersTab({ gymId, count }: { gymId: string; count: number }) {
  return <PeopleTabCard {...MEMBERS_TAB} count={count} query={useGymMembers(gymId)} />;
}

export function GymTrainersTab({ gymId, count }: { gymId: string; count: number }) {
  return <PeopleTabCard {...TRAINERS_TAB} count={count} query={useGymTrainers(gymId)} />;
}

/** Staff rows come from a loosely-typed endpoint; normalise the alternative field names once. */
function toStaffRow(s: any) {
  return {
    key: s.id ?? s.userId,
    name: `${s.firstName ?? s.name ?? '—'} ${s.lastName ?? ''}`,
    email: s.email ?? '—',
    role: (s.staffSubRole ?? s.role ?? '').toLowerCase() || '—',
    phone: s.phone ?? s.phoneNumber ?? '—',
    status: s.status ?? 'active',
  };
}

export function GymStaffTab({ gymId, onAddStaff }: { gymId: string; onAddStaff: () => void }) {
  const { data, isLoading } = useGymStaff(gymId);
  const staff = data?.data ?? [];
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold flex items-center gap-2">
          <Shield className="h-4 w-4" />
          Staff ({staff.length})
        </h3>
        <button
          onClick={onAddStaff}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90"
        >
          <UserPlus className="h-4 w-4" /> Add Staff
        </button>
      </div>
      <PeopleTable
        isLoading={isLoading}
        isEmpty={staff.length === 0}
        emptyText='No staff members at this gym yet. Click "Add Staff" to create one.'
        head={
          <>
            <th className={TH_LEFT}>Name</th>
            <th className={TH_LEFT}>Email</th>
            <th className={TH_LEFT}>Role</th>
            <th className={TH_LEFT}>Phone</th>
            <th className={TH_LEFT}>Status</th>
          </>
        }
      >
        {staff.map(toStaffRow).map((s) => (
          <tr key={s.key} className={ROW}>
            <td className="py-2 px-3 font-medium">{s.name}</td>
            <td className="py-2 px-3 text-muted-foreground">{s.email}</td>
            <td className="py-2 px-3 capitalize">{s.role}</td>
            <td className="py-2 px-3 text-muted-foreground">{s.phone}</td>
            <td className="py-2 px-3">
              <StatusBadge variant={s.status} dot />
            </td>
          </tr>
        ))}
      </PeopleTable>
    </div>
  );
}

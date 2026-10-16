'use client';

import { useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import {
  PageHeader,
  DataTable,
  StatusBadge,
  DateRangePicker,
  formatCurrency,
} from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Plus, Filter, AlertCircle } from 'lucide-react';
import { useGyms, useAdminUsers } from '@/lib/hooks';
import { AddGymModal } from '@/components/add-gym-modal';
import type { GymListItem } from '@gym360/ui';
import { TableSkeleton } from '@gym360/ui';

const columns: ColumnDef<GymListItem, unknown>[] = [
  {
    accessorKey: 'name',
    header: 'Gym Name',
    cell: ({ row }) => (
      <div>
        <p className="font-medium">{row.original.name}</p>
        <p className="text-xs text-muted-foreground">{row.original.gymCode}</p>
      </div>
    ),
  },
  {
    accessorKey: 'city',
    header: 'City',
  },
  {
    accessorKey: 'ownerName',
    header: 'Owner',
  },
  {
    accessorKey: 'planName',
    header: 'Plan',
    cell: ({ row }) => <StatusBadge variant={row.original.planName.toLowerCase()} />,
  },
  {
    accessorKey: 'activeMemberCount',
    header: 'Members',
    cell: ({ row }) => <span className="font-medium">{row.original.activeMemberCount}</span>,
  },
  {
    accessorKey: 'activeTrainerCount',
    header: 'Trainers',
  },
  {
    accessorKey: 'monthlyRevenue',
    header: 'Revenue',
    cell: ({ row }) => <span className="font-medium">{formatCurrency(row.original.monthlyRevenue)}</span>,
  },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => <StatusBadge variant={row.original.status} dot />,
  },
  {
    accessorKey: 'paymentStatus',
    header: 'Payment',
    cell: ({ row }) => <StatusBadge variant={row.original.paymentStatus} />,
  },
];

const STATUS_OPTIONS = [
  { value: 'active', label: 'Active' },
  { value: 'suspended', label: 'Suspended' },
  { value: 'pending', label: 'Pending' },
  { value: 'grace_period', label: 'Grace Period' },
  { value: 'terminated', label: 'Terminated' },
];

type DateRange = { from: Date; to: Date };

// A select filter set to 'all' matches everything.
const matches = (filter: string, value: string) => filter === 'all' || value === filter;

function inDateRange(createdAt: string, range: DateRange | null) {
  if (!range) return true;
  const created = new Date(createdAt);
  return !(created < range.from || created > range.to);
}

const uniqueSorted = (values: string[]) => [...new Set(values)].sort();
const asOptions = (values: string[]) => values.map((value) => ({ value, label: value }));

function FilterSelect({ value, onChange, allLabel, options }: {
  value: string;
  onChange: (value: string) => void;
  allLabel: string;
  options: { value: string; label: string }[];
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
    >
      <option value="all">{allLabel}</option>
      {options.map((o) => (
        <option key={o.value} value={o.value}>{o.label}</option>
      ))}
    </select>
  );
}

export default function GymsPage() {
  const router = useRouter();
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [cityFilter, setCityFilter] = useState<string>('all');
  const [planFilter, setPlanFilter] = useState<string>('all');
  const [dateRange, setDateRange] = useState<DateRange | null>(null);
  const [showAddModal, setShowAddModal] = useState(false);

  const { data: gymsData, isLoading, isError } = useGyms();
  const { data: usersData } = useAdminUsers();
  const allGyms = gymsData?.data ?? [];

  const owners = useMemo(
    () => (usersData?.data ?? []).filter((u) => u.role === 'owner' || u.role === 'gym_owner'),
    [usersData],
  );

  const cities = useMemo(() => uniqueSorted(allGyms.map((g) => g.city)), [allGyms]);
  const plans = useMemo(() => uniqueSorted(allGyms.map((g) => g.planName)), [allGyms]);

  const filteredGyms = useMemo(
    () => allGyms.filter((gym) =>
      matches(statusFilter, gym.status)
      && matches(cityFilter, gym.city)
      && matches(planFilter, gym.planName)
      && inDateRange(gym.createdAt, dateRange)),
    [allGyms, statusFilter, cityFilter, planFilter, dateRange],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title="All Gyms"
        description={isLoading ? 'Loading...' : `${allGyms.length} gyms across Egypt`}
        actions={
          <button
            onClick={() => setShowAddModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" />
            Add Gym
          </button>
        }
      />

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <Filter className="h-4 w-4 text-muted-foreground" />
        <FilterSelect value={statusFilter} onChange={setStatusFilter} allLabel="All Statuses" options={STATUS_OPTIONS} />
        <FilterSelect value={cityFilter} onChange={setCityFilter} allLabel="All Cities" options={asOptions(cities)} />
        <FilterSelect value={planFilter} onChange={setPlanFilter} allLabel="All Plans" options={asOptions(plans)} />
        <DateRangePicker
          value={dateRange ?? undefined}
          onChange={(range) => setDateRange(range)}
          className="ml-auto"
        />
        {dateRange && (
          <button
            onClick={() => setDateRange(null)}
            className="text-xs text-muted-foreground hover:text-foreground underline"
          >
            Clear date
          </button>
        )}
      </div>

      {isError ? (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">Failed to load gyms. Please refresh the page.</p>
        </div>
      ) : isLoading ? (
        <TableSkeleton rows={8} columns={9} />
      ) : (
        <DataTable
          data={filteredGyms}
          columns={columns}
          searchPlaceholder="Search gyms..."
          searchColumn="name"
          enableExport
          onRowClick={(gym) => router.push(`/gyms/${gym.id}`)}
        />
      )}

      <AddGymModal open={showAddModal} owners={owners} onClose={() => setShowAddModal(false)} />
    </div>
  );
}

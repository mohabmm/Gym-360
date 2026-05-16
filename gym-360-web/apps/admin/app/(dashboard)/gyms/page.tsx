'use client';

import { useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import {
  PageHeader,
  DataTable,
  StatusBadge,
  DateRangePicker,
  formatCurrency,
  Modal,
  apiPost,
  GYMS as GYMS_ENDPOINTS,
  useInvalidateQueries,
} from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Plus, Filter, AlertCircle } from 'lucide-react';
import { useGyms, useAdminUsers } from '@/lib/hooks';
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

export default function GymsPage() {
  const router = useRouter();
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [cityFilter, setCityFilter] = useState<string>('all');
  const [planFilter, setPlanFilter] = useState<string>('all');
  const [dateRange, setDateRange] = useState<{ from: Date; to: Date } | null>(null);
  const [showAddModal, setShowAddModal] = useState(false);
  const [newGym, setNewGym] = useState({ name: '', gymCode: '', city: '', ownerId: '', ownerEmail: '' });
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { invalidate } = useInvalidateQueries();

  const { data: gymsData, isLoading, isError } = useGyms();
  const { data: usersData } = useAdminUsers();
  const allGyms = gymsData?.data ?? [];

  const owners = useMemo(
    () => (usersData?.data ?? []).filter((u) => u.role === 'owner' || u.role === 'gym_owner'),
    [usersData],
  );

  const cities = useMemo(() => [...new Set(allGyms.map((g) => g.city))].sort(), [allGyms]);
  const plans = useMemo(() => [...new Set(allGyms.map((g) => g.planName))].sort(), [allGyms]);

  const filteredGyms = useMemo(() => {
    return allGyms.filter((gym) => {
      if (statusFilter !== 'all' && gym.status !== statusFilter) return false;
      if (cityFilter !== 'all' && gym.city !== cityFilter) return false;
      if (planFilter !== 'all' && gym.planName !== planFilter) return false;
      if (dateRange) {
        const created = new Date(gym.createdAt);
        if (created < dateRange.from || created > dateRange.to) return false;
      }
      return true;
    });
  }, [allGyms, statusFilter, cityFilter, planFilter, dateRange]);

  const handleCreateGym = async () => {
    if (!newGym.name.trim() || !newGym.gymCode.trim() || !newGym.ownerId) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      await apiPost(GYMS_ENDPOINTS.CREATE, {
        ownerId: newGym.ownerId,
        gymCode: newGym.gymCode,
        name: newGym.name,
        description: null,
        address: {
          street: 'TBD',
          city: newGym.city || 'Cairo',
          state: 'Cairo',
          country: 'EG',
          zipCode: '00000',
        },
        contact: {
          email: newGym.ownerEmail || 'owner@example.com',
          phone: '+20000000000',
        },
      });
      invalidate(['gyms']);
      setShowAddModal(false);
      setNewGym({ name: '', gymCode: '', city: '', ownerId: '', ownerEmail: '' });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to create gym';
      setSubmitError(msg);
    } finally {
      setSubmitting(false);
    }
  };

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
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
        >
          <option value="all">All Statuses</option>
          <option value="active">Active</option>
          <option value="suspended">Suspended</option>
          <option value="pending">Pending</option>
          <option value="grace_period">Grace Period</option>
          <option value="terminated">Terminated</option>
        </select>
        <select
          value={cityFilter}
          onChange={(e) => setCityFilter(e.target.value)}
          className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
        >
          <option value="all">All Cities</option>
          {cities.map((city) => (
            <option key={city} value={city}>{city}</option>
          ))}
        </select>
        <select
          value={planFilter}
          onChange={(e) => setPlanFilter(e.target.value)}
          className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
        >
          <option value="all">All Plans</option>
          {plans.map((plan) => (
            <option key={plan} value={plan}>{plan}</option>
          ))}
        </select>
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

      <Modal
        open={showAddModal}
        onClose={() => { setShowAddModal(false); setSubmitError(null); }}
        title="Add Gym"
        size="md"
        footer={
          <>
            <button
              onClick={() => { setShowAddModal(false); setSubmitError(null); }}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={handleCreateGym}
              disabled={submitting || !newGym.name.trim() || !newGym.gymCode.trim() || !newGym.ownerId}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {submitting ? 'Creating...' : 'Create Gym'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          {submitError && (
            <div className="rounded-lg bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
              {submitError}
            </div>
          )}
          <div>
            <label className="block text-sm font-medium mb-1.5">Gym Name</label>
            <input
              type="text"
              value={newGym.name}
              onChange={(e) => setNewGym((g) => ({ ...g, name: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Gym Code</label>
            <input
              type="text"
              value={newGym.gymCode}
              onChange={(e) => setNewGym((g) => ({ ...g, gymCode: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">City</label>
            <input
              type="text"
              value={newGym.city}
              onChange={(e) => setNewGym((g) => ({ ...g, city: e.target.value }))}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Owner</label>
            <select
              value={newGym.ownerId}
              onChange={(e) => {
                const owner = owners.find((o) => o.id === e.target.value);
                setNewGym((g) => ({ ...g, ownerId: e.target.value, ownerEmail: owner?.email ?? '' }));
              }}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
            >
              <option value="">Select an owner...</option>
              {owners.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.firstName} {o.lastName} ({o.email})
                </option>
              ))}
            </select>
            {owners.length === 0 && (
              <p className="text-xs text-muted-foreground mt-1">No gym owner accounts found. Create an owner account first.</p>
            )}
          </div>
        </div>
      </Modal>
    </div>
  );
}

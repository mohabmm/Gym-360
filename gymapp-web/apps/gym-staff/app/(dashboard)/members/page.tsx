'use client';

import { useState, useMemo, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { PageHeader, DataTable, StatusBadge, Modal, formatDate } from '@gymapp/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Plus, Filter, Upload } from 'lucide-react';
import { useMembers, useCreateMember } from '@/lib/hooks/useMembers';
import type { MemberListItem } from '@gymapp/ui';

const columns: ColumnDef<MemberListItem, unknown>[] = [
  {
    accessorKey: 'name',
    header: 'Name',
    cell: ({ row }) => (
      <div className="flex items-center gap-3">
        <div className="h-9 w-9 rounded-full bg-primary/10 flex items-center justify-center text-xs font-bold text-primary">
          {(row.original.name ?? '').split(' ').map((n) => n[0]).join('').slice(0, 2)}
        </div>
        <div>
          <p className="font-medium">{row.original.name}</p>
          <p className="text-xs text-muted-foreground">{row.original.nameAr}</p>
        </div>
      </div>
    ),
  },
  { accessorKey: 'phone', header: 'Phone' },
  {
    accessorKey: 'membershipType',
    header: 'Plan',
    cell: ({ row }) => <span className="capitalize">{(row.original.membershipType ?? '').replace(/_/g, ' ')}</span>,
  },
  { accessorKey: 'section', header: 'Floor' },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => <StatusBadge variant={row.original.status} dot />,
  },
  {
    accessorKey: 'expiryDate',
    header: 'Expires',
    cell: ({ row }) => formatDate(row.original.expiryDate),
  },
  {
    accessorKey: 'lastCheckIn',
    header: 'Last Check-in',
    cell: ({ row }) => row.original.lastCheckIn ? formatDate(row.original.lastCheckIn) : 'Never',
  },
];

export default function MembersPage() {
  const router = useRouter();
  const { data: members = [], isLoading } = useMembers();
  const createMember = useCreateMember();
  const [statusFilter, setStatusFilter] = useState('all');
  const [sectionFilter, setSectionFilter] = useState('all');
  const [showAddMember, setShowAddMember] = useState(false);
  const [newMember, setNewMember] = useState({ name: '', email: '', phone: '', section: 'Mens' });
  const [formError, setFormError] = useState<string | null>(null);
  const importInputRef = useRef<HTMLInputElement>(null);

  const resetForm = () => {
    setNewMember({ name: '', email: '', phone: '', section: 'Mens' });
    setFormError(null);
  };

  const handleAddMember = () => {
    if (!newMember.name.trim() || !newMember.phone.trim()) {
      setFormError('Name and phone are required.');
      return;
    }
    setFormError(null);
    createMember.mutate(newMember, {
      onSuccess: () => {
        toast.success(`Member ${newMember.name} added.`);
        resetForm();
        setShowAddMember(false);
      },
      onError: (err: unknown) => {
        const msg = (err as { response?: { data?: { errors?: { message?: string }[] } } })
          ?.response?.data?.errors?.[0]?.message ?? 'Failed to add member.';
        setFormError(msg);
      },
    });
  };

  const handleBulkImport = () => importInputRef.current?.click();
  const handleImportFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    toast.success(`Processing ${file.name}…`);
    e.target.value = '';
  };

  const filtered = useMemo(() => {
    return members.filter((m) => {
      if (statusFilter !== 'all' && m.status !== statusFilter) return false;
      if (sectionFilter !== 'all' && m.section !== sectionFilter) return false;
      return true;
    });
  }, [members, statusFilter, sectionFilter]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <p className="text-muted-foreground text-sm">Loading members…</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Members"
        description={`${members.length} members at First Gym`}
        actions={
          <div className="flex items-center gap-2">
            <input ref={importInputRef} type="file" accept=".csv,.xlsx" className="hidden" onChange={handleImportFile} />
            <button onClick={handleBulkImport} className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium hover:bg-accent">
              <Upload className="h-4 w-4" /> Bulk Import
            </button>
            <button onClick={() => setShowAddMember(true)} className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">
              <Plus className="h-4 w-4" /> Add Member
            </button>
          </div>
        }
      />

      <div className="flex items-center gap-3">
        <Filter className="h-4 w-4 text-muted-foreground" />
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
          <option value="all">All Statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
          <option value="frozen">Frozen</option>
          <option value="expired">Expired</option>
        </select>
        <select value={sectionFilter} onChange={(e) => setSectionFilter(e.target.value)} className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
          <option value="all">All Floors</option>
          <option value="Mens">Men&apos;s</option>
          <option value="Womens">Ladies</option>
          <option value="Mixed">Mixed</option>
        </select>
      </div>

      <DataTable
        data={filtered}
        columns={columns}
        searchPlaceholder="Search members by name..."
        searchColumn="name"
        enableExport
        onRowClick={(member) => router.push(`/members/${member.id}`)}
      />

      <Modal
        open={showAddMember}
        onClose={() => { resetForm(); setShowAddMember(false); }}
        title="Add Member"
        size="md"
        footer={
          <>
            <button onClick={() => { resetForm(); setShowAddMember(false); }} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={handleAddMember} disabled={createMember.isPending} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">
              {createMember.isPending ? 'Adding…' : 'Add Member'}
            </button>
          </>
        }
      >
        <div className="space-y-3">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <label className="block">
            <span className="text-sm font-medium">Name</span>
            <input value={newMember.name} onChange={(e) => setNewMember({ ...newMember, name: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Phone</span>
            <input type="tel" value={newMember.phone} onChange={(e) => setNewMember({ ...newMember, phone: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Email</span>
            <input type="email" value={newMember.email} onChange={(e) => setNewMember({ ...newMember, email: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Floor</span>
            <select value={newMember.section} onChange={(e) => setNewMember({ ...newMember, section: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
              <option value="Mens">Men&apos;s</option>
              <option value="Womens">Ladies</option>
              <option value="Mixed">Mixed</option>
            </select>
          </label>
        </div>
      </Modal>
    </div>
  );
}

'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { PageHeader, DataTable, StatusBadge, Modal, formatDate } from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Plus, Filter, Upload } from 'lucide-react';
import {
  useMembersPaged,
  useCreateMember,
  useLookupMemberByPhone,
  useLinkExistingMember,
  type ExistingMemberLookup,
} from '@/lib/hooks/useMembers';
import type { MemberListItem } from '@gym360/ui';

const PAGE_SIZE = 20;

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
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sectionFilter, setSectionFilter] = useState('all');

  // Debounce the search box so each keystroke doesn't fire a request; reset to
  // page 1 whenever the term settles so results start from the top.
  useEffect(() => {
    const t = setTimeout(() => {
      setSearch(searchInput);
      setPage(1);
    }, 400);
    return () => clearTimeout(t);
  }, [searchInput]);

  const { data: membersPage, isLoading, isFetching } = useMembersPaged({
    page,
    pageSize: PAGE_SIZE,
    search,
    status: statusFilter,
    section: sectionFilter,
  });
  const members = membersPage?.items ?? [];
  const totalCount = membersPage?.totalCount ?? 0;

  const createMember = useCreateMember();
  const lookupMember = useLookupMemberByPhone();
  const linkExisting = useLinkExistingMember();
  const [showAddMember, setShowAddMember] = useState(false);
  const [newMember, setNewMember] = useState({ name: '', email: '', phone: '', section: 'Mens' });
  const [formError, setFormError] = useState<string | null>(null);
  // Set when staff-create reports a duplicate (409) AND we resolved who the phone
  // belongs to — drives the "Add existing member?" confirm prompt inside the modal.
  const [existingMatch, setExistingMatch] = useState<ExistingMemberLookup | null>(null);
  const importInputRef = useRef<HTMLInputElement>(null);

  const resetForm = () => {
    setNewMember({ name: '', email: '', phone: '', section: 'Mens' });
    setFormError(null);
    setExistingMatch(null);
  };

  const handleAddMember = () => {
    if (!newMember.name.trim() || !newMember.phone.trim()) {
      setFormError('Name and phone are required.');
      return;
    }
    setFormError(null);
    setExistingMatch(null);
    createMember.mutate(newMember, {
      onSuccess: () => {
        toast.success(`Member ${newMember.name} added.`);
        resetForm();
        setShowAddMember(false);
      },
      onError: (err: unknown) => {
        const status = (err as { response?: { status?: number } })?.response?.status;
        const msg = (err as { response?: { data?: { errors?: { message?: string }[] } } })
          ?.response?.data?.errors?.[0]?.message ?? 'Failed to add member.';
        // 409 = phone/email already belongs to an existing member. Resolve their
        // identity so staff can confirm and re-link instead of creating a duplicate.
        if (status === 409) {
          lookupMember.mutate(newMember.phone, {
            onSuccess: (match) => { setFormError(null); setExistingMatch(match); },
            onError: () => setFormError(msg),
          });
          return;
        }
        setFormError(msg);
      },
    });
  };

  const handleLinkExisting = () => {
    if (!existingMatch) return;
    setFormError(null);
    linkExisting.mutate(
      { phone: newMember.phone, section: newMember.section },
      {
        onSuccess: () => {
          toast.success(`${existingMatch.firstName} ${existingMatch.lastName} added to your gym.`);
          resetForm();
          setShowAddMember(false);
        },
        onError: (err: unknown) => {
          const msg = (err as { response?: { data?: { errors?: { message?: string }[] } } })
            ?.response?.data?.errors?.[0]?.message ?? 'Failed to add existing member.';
          setFormError(msg);
        },
      },
    );
  };

  const handleBulkImport = () => importInputRef.current?.click();
  const handleImportFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    toast.success(`Processing ${file.name}…`);
    e.target.value = '';
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Members"
        description={`${totalCount} members`}
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
        <select value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }} className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
          <option value="all">All Statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
          <option value="frozen">Frozen</option>
          <option value="expired">Expired</option>
        </select>
        <select value={sectionFilter} onChange={(e) => { setSectionFilter(e.target.value); setPage(1); }} className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
          <option value="all">All Floors</option>
          <option value="Mens">Men&apos;s</option>
          <option value="Womens">Ladies</option>
          <option value="Mixed">Mixed</option>
        </select>
      </div>

      <DataTable
        data={members}
        columns={columns}
        searchPlaceholder="Search by name, phone, email or member #..."
        onSearchChange={setSearchInput}
        searchValue={searchInput}
        serverSidePagination
        totalCount={totalCount}
        page={page}
        pageSize={PAGE_SIZE}
        onPageChange={setPage}
        isLoading={isLoading || isFetching}
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
            {existingMatch ? (
              <button
                onClick={handleLinkExisting}
                disabled={linkExisting.isPending || existingMatch.isAlreadyMemberOfThisGym}
                className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
              >
                {linkExisting.isPending ? 'Adding…' : 'Add existing member'}
              </button>
            ) : (
              <button onClick={handleAddMember} disabled={createMember.isPending || lookupMember.isPending} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">
                {createMember.isPending || lookupMember.isPending ? 'Adding…' : 'Add Member'}
              </button>
            )}
          </>
        }
      >
        <div className="space-y-3">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          {existingMatch && (
            <div className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm">
              <p className="font-medium text-amber-900">
                {existingMatch.firstName} {existingMatch.lastName} is already registered
              </p>
              <p className="text-amber-800">
                {existingMatch.email} · {existingMatch.phone}
              </p>
              <p className="mt-1 text-amber-800">
                {existingMatch.isAlreadyMemberOfThisGym
                  ? 'This person is already a member of your gym.'
                  : 'Add this existing member to your gym instead of creating a duplicate?'}
              </p>
            </div>
          )}
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

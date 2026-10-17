'use client';

import { useState } from 'react';
import { PageHeader } from '@gym360/ui';
import { Plus } from 'lucide-react';
import { useStaff } from '@/lib/hooks/useStaff';
import { StaffTable, PermissionMatrix } from '@/components/staff/StaffSections';
import { AddStaffModal } from '@/components/staff/AddStaffModal';

export default function StaffPage() {
  const { data: gymStaff = [], isLoading } = useStaff();
  const [showAddStaff, setShowAddStaff] = useState(false);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Staff"
        description={`${gymStaff.length} staff members at First Gym`}
        actions={
          <button onClick={() => setShowAddStaff(true)} className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">
            <Plus className="h-4 w-4" /> Add Staff
          </button>
        }
      />

      {/* Staff Table */}
      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-sm text-muted-foreground">Loading staff...</div>
        ) : (
          <StaffTable staff={gymStaff} />
        )}
      </div>

      {/* Permission Matrix */}
      <PermissionMatrix staff={gymStaff} />

      <AddStaffModal open={showAddStaff} onClose={() => setShowAddStaff(false)} />
    </div>
  );
}

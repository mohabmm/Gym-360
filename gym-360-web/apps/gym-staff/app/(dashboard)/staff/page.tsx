'use client';

import { useState } from 'react';
import { toast } from 'sonner';
import { PageHeader, StatusBadge, Modal } from '@gymapp/ui';
import { Plus, Shield } from 'lucide-react';
import { useStaff, useCreateStaff } from '@/lib/hooks/useStaff';
import type { StaffPermission } from '@gymapp/ui';

const allPermissions: { key: StaffPermission; label: string }[] = [
  { key: 'manage_members', label: 'Manage Members' },
  { key: 'manage_trainers', label: 'Manage Trainers' },
  { key: 'manage_billing', label: 'Manage Billing' },
  { key: 'manage_classes', label: 'Manage Classes' },
  { key: 'manage_staff', label: 'Manage Staff' },
  { key: 'manage_settings', label: 'Manage Settings' },
  { key: 'view_analytics', label: 'View Analytics' },
  { key: 'manage_announcements', label: 'Manage Announcements' },
  { key: 'manage_check_ins', label: 'Manage Check-ins' },
];

export default function StaffPage() {
  const { data: gymStaff = [], isLoading } = useStaff();
  const createStaff = useCreateStaff();

  const [showAddStaff, setShowAddStaff] = useState(false);
  const [newStaff, setNewStaff] = useState({ firstName: '', lastName: '', email: '', phone: '', password: '', role: 'manager' });
  const [formError, setFormError] = useState<string | null>(null);

  const resetForm = () => {
    setNewStaff({ firstName: '', lastName: '', email: '', phone: '', password: '', role: 'manager' });
    setFormError(null);
  };

  const handleAddStaff = () => {
    if (!newStaff.firstName.trim() || !newStaff.lastName.trim() || !newStaff.email.trim() || !newStaff.password.trim()) {
      setFormError('First name, last name, email, and password are required.');
      return;
    }
    createStaff.mutate(
      {
        firstName: newStaff.firstName,
        lastName: newStaff.lastName,
        email: newStaff.email,
        phone: newStaff.phone,
        password: newStaff.password,
        staffSubRole: newStaff.role,
        permissions: [],
      },
      {
        onSuccess: () => {
          toast.success(`Invitation sent to ${newStaff.firstName} ${newStaff.lastName} (${newStaff.email}).`);
          resetForm();
          setShowAddStaff(false);
        },
        onError: () => {
          setFormError('Failed to add staff member. Please try again.');
        },
      },
    );
  };

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
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Name</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Role</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Email</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Phone</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Status</th>
              </tr>
            </thead>
            <tbody>
              {gymStaff.map((staff) => (
                <tr key={staff.id} className="border-b last:border-0 hover:bg-muted/50">
                  <td className="py-3 px-4">
                    <div className="flex items-center gap-3">
                      <div className="h-9 w-9 rounded-full bg-primary/10 flex items-center justify-center text-xs font-bold text-primary">
                        {staff.firstName?.[0]}{staff.lastName?.[0]}
                      </div>
                      <div>
                        <p className="font-medium">{staff.firstName} {staff.lastName}</p>
                        <p className="text-xs text-muted-foreground">{staff.firstNameAr} {staff.lastNameAr}</p>
                      </div>
                    </div>
                  </td>
                  <td className="py-3 px-4"><StatusBadge variant={staff.staffSubRole} /></td>
                  <td className="py-3 px-4">{staff.email}</td>
                  <td className="py-3 px-4">{staff.phone}</td>
                  <td className="py-3 px-4"><StatusBadge variant={staff.status} dot /></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Permission Matrix */}
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
          <Shield className="h-4 w-4 text-primary" />
          Permission Matrix
        </h3>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Permission</th>
                {gymStaff.map((s) => (
                  <th key={s.id} className="text-center py-2 px-3 font-semibold text-muted-foreground">{s.firstName}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {allPermissions.map((perm) => (
                <tr key={perm.key} className="border-b last:border-0">
                  <td className="py-2 px-3">{perm.label}</td>
                  {gymStaff.map((s) => (
                    <td key={s.id} className="py-2 px-3 text-center">
                      {(s.permissions ?? []).includes(perm.key) ? (
                        <div className="h-5 w-5 rounded bg-emerald-100 flex items-center justify-center mx-auto">
                          <svg className="h-3 w-3 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
                        </div>
                      ) : (
                        <div className="h-5 w-5 rounded bg-gray-100 flex items-center justify-center mx-auto">
                          <svg className="h-3 w-3 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                        </div>
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={showAddStaff}
        onClose={() => { resetForm(); setShowAddStaff(false); }}
        title="Add Staff Member"
        size="md"
        footer={
          <>
            <button onClick={() => { resetForm(); setShowAddStaff(false); }} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={handleAddStaff} disabled={createStaff.isPending} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">
              {createStaff.isPending ? 'Adding...' : 'Add Staff'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1.5">First Name</label>
              <input type="text" value={newStaff.firstName} onChange={(e) => setNewStaff({ ...newStaff, firstName: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1.5">Last Name</label>
              <input type="text" value={newStaff.lastName} onChange={(e) => setNewStaff({ ...newStaff, lastName: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Email</label>
            <input type="email" value={newStaff.email} onChange={(e) => setNewStaff({ ...newStaff, email: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Phone</label>
            <input type="tel" value={newStaff.phone} onChange={(e) => setNewStaff({ ...newStaff, phone: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Initial Password</label>
            <input type="password" value={newStaff.password} onChange={(e) => setNewStaff({ ...newStaff, password: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" placeholder="Will be sent to staff member" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Role</label>
            <select value={newStaff.role} onChange={(e) => setNewStaff({ ...newStaff, role: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
              <option value="manager">Manager</option>
              <option value="receptionist">Receptionist</option>
            </select>
          </div>
        </div>
      </Modal>
    </div>
  );
}

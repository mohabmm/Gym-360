import { StatusBadge } from '@gym360/ui';
import { Shield } from 'lucide-react';
import type { StaffMember, StaffPermission, StaffSubRole } from '@gym360/ui';

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

// Default role permission matrix (docs/03b §"Permission Matrix", docs/06 #20):
//   Owner        = all
//   Manager      = all except billing settings
//   Receptionist = check-in + view members
// Invariant: receptionist ⊆ manager ⊆ owner. The docs "receptionist records
// payments" capability is a billing *transaction*, not the coarse "Manage
// Billing" authority (which the manager also lacks), so it is not granted
// here — granting it would invert the role hierarchy.
// These defaults apply unless the gym is on Business (custom_permissions),
// where the BE returns per-staff overrides which we honour below.
const DEFAULT_PERMISSIONS: Record<StaffSubRole, StaffPermission[]> = {
  owner: allPermissions.map((p) => p.key),
  manager: allPermissions.map((p) => p.key).filter((k) => k !== 'manage_billing'),
  receptionist: ['manage_check_ins', 'manage_members'],
};

function effectivePermissions(s: StaffMember): StaffPermission[] {
  return s.permissions?.length ? s.permissions : (DEFAULT_PERMISSIONS[s.staffSubRole] ?? []);
}

function PermissionCell({ granted }: { granted: boolean }) {
  return granted ? (
    <div className="h-5 w-5 rounded bg-emerald-100 flex items-center justify-center mx-auto">
      <svg className="h-3 w-3 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
    </div>
  ) : (
    <div className="h-5 w-5 rounded bg-gray-100 flex items-center justify-center mx-auto">
      <svg className="h-3 w-3 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
    </div>
  );
}

export function StaffTable({ staff: gymStaff }: { staff: StaffMember[] }) {
  return (
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
  );
}

export function PermissionMatrix({ staff: gymStaff }: { staff: StaffMember[] }) {
  return (
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
                    <PermissionCell granted={effectivePermissions(s).includes(perm.key)} />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

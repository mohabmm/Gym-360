'use client';

import { useState, type ReactNode } from 'react';
import { Modal, extractApiErrors } from '@gym360/ui';
import { AlertCircle } from 'lucide-react';
import { useCreateGymStaff, type CreateGymStaffPayload } from '@/lib/hooks';

const EMPTY_STAFF_FORM: CreateGymStaffPayload = {
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phone: '',
  staffSubRole: 'Receptionist',
};

const INPUT_CLASS = 'h-9 w-full rounded-lg border border-input bg-background px-3 text-sm';

function StaffField({ label, required = false, hint, children }: { label: string; required?: boolean; hint?: string; children: ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium">
        {label}
        {required && <> <span className="text-red-500">*</span></>}
      </label>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted-foreground">{hint}</span>}
    </div>
  );
}

type StaffFieldUpdater = <K extends keyof CreateGymStaffPayload>(key: K, value: CreateGymStaffPayload[K]) => void;

function StaffFormFields({ form, update }: { form: CreateGymStaffPayload; update: StaffFieldUpdater }) {
  return (
    <>
      <div className="grid grid-cols-2 gap-3">
        <StaffField label="First Name" required>
          <input
            value={form.firstName}
            onChange={(e) => update('firstName', e.target.value)}
            required maxLength={100}
            className={INPUT_CLASS}
          />
        </StaffField>
        <StaffField label="Last Name" required>
          <input
            value={form.lastName}
            onChange={(e) => update('lastName', e.target.value)}
            required maxLength={100}
            className={INPUT_CLASS}
          />
        </StaffField>
      </div>

      <StaffField label="Login Email" required>
        <input
          type="email"
          value={form.email}
          onChange={(e) => update('email', e.target.value)}
          required
          className={INPUT_CLASS}
        />
      </StaffField>

      <StaffField label="Password" required hint="Min 8 characters. Staff can change after first login.">
        <input
          type="password"
          value={form.password}
          onChange={(e) => update('password', e.target.value)}
          required minLength={8}
          className={INPUT_CLASS}
        />
      </StaffField>

      <StaffField label="Phone" hint="Optional. Egyptian local (01XXXXXXXXX) or E.164 (+20...).">
        <input
          type="tel"
          value={form.phone ?? ''}
          onChange={(e) => update('phone', e.target.value)}
          className={INPUT_CLASS}
        />
      </StaffField>

      <StaffField label="Staff Role" required>
        <select
          value={form.staffSubRole}
          onChange={(e) => update('staffSubRole', e.target.value as CreateGymStaffPayload['staffSubRole'])}
          className={INPUT_CLASS}
        >
          <option value="Owner">Owner</option>
          <option value="Manager">Manager</option>
          <option value="Receptionist">Receptionist</option>
        </select>
      </StaffField>
    </>
  );
}

interface AddStaffModalProps {
  gymId: string;
  gymName: string;
  open: boolean;
  onClose: () => void;
}

export function AddStaffModal({ gymId, gymName, open, onClose }: AddStaffModalProps) {
  const createStaffMutation = useCreateGymStaff(gymId);
  const [staffForm, setStaffForm] = useState<CreateGymStaffPayload>(EMPTY_STAFF_FORM);
  const [staffError, setStaffError] = useState<string | null>(null);
  const isPending = createStaffMutation.isPending;

  const updateStaffField = <K extends keyof CreateGymStaffPayload>(key: K, value: CreateGymStaffPayload[K]) => {
    setStaffForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmitStaff = async (e: React.FormEvent) => {
    e.preventDefault();
    setStaffError(null);
    try {
      await createStaffMutation.mutateAsync(staffForm);
      onClose();
      setStaffForm(EMPTY_STAFF_FORM);
    } catch (err) {
      const apiErrors = extractApiErrors(err);
      setStaffError(apiErrors[0]?.message ?? 'Failed to create staff member.');
    }
  };

  const closeStaffModal = () => {
    if (isPending) return;
    onClose();
    setStaffForm(EMPTY_STAFF_FORM);
    setStaffError(null);
  };

  return (
    <Modal
      open={open}
      onClose={closeStaffModal}
      title={`Add Staff to ${gymName}`}
      description="Creates a User (role=GymStaff, activated), enrolls them in this gym, and assigns a StaffRole. The staff member can sign in immediately."
      size="lg"
      footer={
        <>
          <button
            type="button"
            onClick={closeStaffModal}
            disabled={isPending}
            className="rounded-lg border px-3 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            form="add-gym-staff-form"
            disabled={isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          >
            {isPending ? 'Creating...' : 'Create Staff'}
          </button>
        </>
      }
    >
      <form id="add-gym-staff-form" onSubmit={handleSubmitStaff} className="space-y-4">
        {staffError && (
          <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            <AlertCircle className="h-4 w-4 shrink-0" />
            {staffError}
          </div>
        )}

        <StaffFormFields form={staffForm} update={updateStaffField} />
      </form>
    </Modal>
  );
}

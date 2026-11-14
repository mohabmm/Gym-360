'use client';

import { useState } from 'react';
import { AlertCircle } from 'lucide-react';
import { Modal, extractApiErrors, useInvalidateQueries } from '@gym360/ui';
import { useCreateGymOwner, type CreateGymOwnerPayload } from '@/lib/hooks';

const EMPTY_OWNER_FORM: CreateGymOwnerPayload = {
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phone: '',
  companyName: '',
  billingEmail: '',
  planTier: 'Pro',
  dashboardMode: 'Both',
};

/** "Add Gym Owner" dialog: creates User + Owner + Contract in one call. */
export function AddGymOwnerModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { invalidate } = useInvalidateQueries();
  const [ownerForm, setOwnerForm] = useState<CreateGymOwnerPayload>(EMPTY_OWNER_FORM);
  const [ownerError, setOwnerError] = useState<string | null>(null);
  const createOwnerMutation = useCreateGymOwner();

  const updateOwnerField = <K extends keyof CreateGymOwnerPayload>(key: K, value: CreateGymOwnerPayload[K]) => {
    setOwnerForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmitOwner = async (e: React.FormEvent) => {
    e.preventDefault();
    setOwnerError(null);
    try {
      await createOwnerMutation.mutateAsync({
        ...ownerForm,
        phone: ownerForm.phone?.trim() ? ownerForm.phone.trim() : undefined,
      });
      invalidate(['admin-users']);
      onClose();
      setOwnerForm(EMPTY_OWNER_FORM);
    } catch (err) {
      const apiErrors = extractApiErrors(err);
      setOwnerError(apiErrors[0]?.message ?? 'Failed to create gym owner.');
    }
  };

  const handleCloseOwnerModal = () => {
    if (createOwnerMutation.isPending) return;
    onClose();
    setOwnerError(null);
    setOwnerForm(EMPTY_OWNER_FORM);
  };

  return (
    <Modal
      open={open}
      onClose={handleCloseOwnerModal}
      title="Add Gym Owner"
      description="Creates a User (role=GymOwner, activated), an Owner record, and a Contract row. The owner can sign in immediately. Gyms are added afterwards from the Gyms page."
      size="lg"
      footer={
        <>
          <button
            type="button"
            onClick={handleCloseOwnerModal}
            disabled={createOwnerMutation.isPending}
            className="rounded-lg border border-input bg-background px-3 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            form="create-owner-form"
            disabled={createOwnerMutation.isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {createOwnerMutation.isPending ? 'Creating...' : 'Create Owner'}
          </button>
        </>
      }
    >
      <form id="create-owner-form" onSubmit={handleSubmitOwner} className="space-y-4">
        {ownerError && (
          <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            <AlertCircle className="h-4 w-4 shrink-0" />
            {ownerError}
          </div>
        )}

        <OwnerFormFields form={ownerForm} onChange={updateOwnerField} />
      </form>
    </Modal>
  );
}

type OwnerFieldUpdater = <K extends keyof CreateGymOwnerPayload>(key: K, value: CreateGymOwnerPayload[K]) => void;

function OwnerFormFields({ form: ownerForm, onChange: updateOwnerField }: { form: CreateGymOwnerPayload; onChange: OwnerFieldUpdater }) {
  return (
    <>
      <div className="grid grid-cols-2 gap-3">
        <Field label="First Name" required>
          <input
            value={ownerForm.firstName}
            onChange={(e) => updateOwnerField('firstName', e.target.value)}
            required
            maxLength={100}
            className={inputClass}
          />
        </Field>
        <Field label="Last Name" required>
          <input
            value={ownerForm.lastName}
            onChange={(e) => updateOwnerField('lastName', e.target.value)}
            required
            maxLength={100}
            className={inputClass}
          />
        </Field>
      </div>

      <Field label="Login Email" required>
        <input
          type="email"
          value={ownerForm.email}
          onChange={(e) => updateOwnerField('email', e.target.value)}
          required
          className={inputClass}
        />
      </Field>

      <Field label="Password" required hint="Min 8 characters. Owner can change after first login.">
        <input
          type="password"
          value={ownerForm.password}
          onChange={(e) => updateOwnerField('password', e.target.value)}
          required
          minLength={8}
          className={inputClass}
        />
      </Field>

      <Field label="Phone" hint="Optional. Egyptian local (01XXXXXXXXX) or E.164 (+20...).">
        <input
          type="tel"
          value={ownerForm.phone ?? ''}
          onChange={(e) => updateOwnerField('phone', e.target.value)}
          className={inputClass}
        />
      </Field>

      <hr className="border-border" />

      <Field label="Company / Brand Name" required>
        <input
          value={ownerForm.companyName}
          onChange={(e) => updateOwnerField('companyName', e.target.value)}
          required
          maxLength={200}
          className={inputClass}
        />
      </Field>

      <Field label="Billing Email" required>
        <input
          type="email"
          value={ownerForm.billingEmail}
          onChange={(e) => updateOwnerField('billingEmail', e.target.value)}
          required
          className={inputClass}
        />
      </Field>

      <div className="grid grid-cols-2 gap-3">
        <Field label="Plan Tier" required>
          <select
            value={ownerForm.planTier}
            onChange={(e) => updateOwnerField('planTier', e.target.value as CreateGymOwnerPayload['planTier'])}
            className={inputClass}
          >
            <option value="Free">Free</option>
            <option value="Starter">Starter</option>
            <option value="Pro">Pro</option>
            <option value="Business">Business</option>
          </select>
        </Field>
        <Field label="Dashboard Mode" required>
          <select
            value={ownerForm.dashboardMode}
            onChange={(e) => updateOwnerField('dashboardMode', e.target.value as CreateGymOwnerPayload['dashboardMode'])}
            className={inputClass}
          >
            <option value="Unified">Unified</option>
            <option value="PerGym">Per-Gym</option>
            <option value="Both">Both</option>
          </select>
        </Field>
      </div>
    </>
  );
}

const inputClass = 'h-9 w-full rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30';

function Field({
  label,
  required,
  hint,
  children,
}: {
  label: string;
  required?: boolean;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-foreground">
        {label} {required && <span className="text-red-500">*</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted-foreground">{hint}</span>}
    </label>
  );
}

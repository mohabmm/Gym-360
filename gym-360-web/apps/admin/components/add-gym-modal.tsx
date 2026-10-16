import { useState } from 'react';
import { Modal, apiPost, GYMS as GYMS_ENDPOINTS, useInvalidateQueries } from '@gym360/ui';

interface Owner {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
}

const EMPTY_GYM = { name: '', city: '', ownerId: '', ownerEmail: '' };
const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

// The admin form only collects name/city/owner; the BE requires a full
// address and contact, so the rest are placeholders the owner fills in later.
function toCreateGymRequest(gym: typeof EMPTY_GYM) {
  return {
    ownerId: gym.ownerId,
    name: gym.name,
    description: null,
    address: {
      street: 'TBD',
      city: gym.city || 'Cairo',
      state: 'Cairo',
      country: 'EG',
      zipCode: '00000',
    },
    contact: {
      email: gym.ownerEmail || 'owner@example.com',
      phone: '+20000000000',
    },
  };
}

function errorMessage(err: unknown) {
  return err instanceof Error ? err.message : 'Failed to create gym';
}

interface FooterProps {
  submitting: boolean;
  canSubmit: boolean;
  onCancel: () => void;
  onSubmit: () => void;
}

function AddGymFooter({ submitting, canSubmit, onCancel, onSubmit }: FooterProps) {
  return (
    <>
      <button
        onClick={onCancel}
        className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
      >
        Cancel
      </button>
      <button
        onClick={onSubmit}
        disabled={submitting || !canSubmit}
        className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
      >
        {submitting ? 'Creating...' : 'Create Gym'}
      </button>
    </>
  );
}

interface OwnerSelectProps {
  value: string;
  owners: Owner[];
  onChange: (ownerId: string) => void;
}

function OwnerSelect({ value, owners, onChange }: OwnerSelectProps) {
  return (
    <div>
      <label className="block text-sm font-medium mb-1.5">Owner</label>
      <select value={value} onChange={(e) => onChange(e.target.value)} className={INPUT_CLASS}>
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
  );
}

interface Props {
  open: boolean;
  owners: Owner[];
  onClose: () => void;
}

export function AddGymModal({ open, owners, onClose }: Props) {
  const [newGym, setNewGym] = useState(EMPTY_GYM);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { invalidate } = useInvalidateQueries();
  const canSubmit = !!newGym.name.trim() && !!newGym.ownerId;

  const close = () => {
    onClose();
    setSubmitError(null);
  };

  const handleCreateGym = async () => {
    if (!canSubmit) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      await apiPost(GYMS_ENDPOINTS.CREATE, toCreateGymRequest(newGym));
      invalidate(['gyms']);
      onClose();
      setNewGym(EMPTY_GYM);
    } catch (err: unknown) {
      setSubmitError(errorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const selectOwner = (ownerId: string) => {
    const owner = owners.find((o) => o.id === ownerId);
    setNewGym((g) => ({ ...g, ownerId, ownerEmail: owner?.email ?? '' }));
  };

  return (
    <Modal
      open={open}
      onClose={close}
      title="Add Gym"
      size="md"
      footer={
        <AddGymFooter
          submitting={submitting}
          canSubmit={canSubmit}
          onCancel={close}
          onSubmit={handleCreateGym}
        />
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
            className={INPUT_CLASS}
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1.5">City</label>
          <input
            type="text"
            value={newGym.city}
            onChange={(e) => setNewGym((g) => ({ ...g, city: e.target.value }))}
            className={INPUT_CLASS}
          />
        </div>
        <OwnerSelect value={newGym.ownerId} owners={owners} onChange={selectOwner} />
      </div>
    </Modal>
  );
}

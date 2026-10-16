import { useState } from 'react';
import { toast } from 'sonner';
import { Modal } from '@gym360/ui';
import { useCreateStaff } from '@/lib/hooks/useStaff';

const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

const EMPTY_STAFF = { firstName: '', lastName: '', email: '', phone: '', password: '', role: 'manager' };
type NewStaff = typeof EMPTY_STAFF;

const REQUIRED_FIELDS: (keyof NewStaff)[] = ['firstName', 'lastName', 'email', 'password'];

const hasRequiredFields = (s: NewStaff) => REQUIRED_FIELDS.every((k) => s[k].trim() !== '');

function TextField({ label, type = 'text', value, onChange, placeholder }: {
  label: string;
  type?: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
}) {
  return (
    <div>
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      <input type={type} value={value} onChange={(e) => onChange(e.target.value)} className={INPUT_CLASS} placeholder={placeholder} />
    </div>
  );
}

export function AddStaffModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const createStaff = useCreateStaff();
  const [newStaff, setNewStaff] = useState<NewStaff>(EMPTY_STAFF);
  const [formError, setFormError] = useState<string | null>(null);
  const set = (key: keyof NewStaff) => (value: string) => setNewStaff({ ...newStaff, [key]: value });

  const resetForm = () => {
    setNewStaff(EMPTY_STAFF);
    setFormError(null);
  };

  const close = () => {
    resetForm();
    onClose();
  };

  const handleAddStaff = () => {
    if (!hasRequiredFields(newStaff)) {
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
          close();
        },
        onError: () => {
          setFormError('Failed to add staff member. Please try again.');
        },
      },
    );
  };

  return (
    <Modal
      open={open}
      onClose={close}
      title="Add Staff Member"
      size="md"
      footer={
        <>
          <button onClick={close} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
          <button onClick={handleAddStaff} disabled={createStaff.isPending} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">
            {createStaff.isPending ? 'Adding...' : 'Add Staff'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        {formError && <p className="text-sm text-destructive">{formError}</p>}
        <div className="grid grid-cols-2 gap-4">
          <TextField label="First Name" value={newStaff.firstName} onChange={set('firstName')} />
          <TextField label="Last Name" value={newStaff.lastName} onChange={set('lastName')} />
        </div>
        <TextField label="Email" type="email" value={newStaff.email} onChange={set('email')} />
        <TextField label="Phone" type="tel" value={newStaff.phone} onChange={set('phone')} />
        <TextField label="Initial Password" type="password" value={newStaff.password} onChange={set('password')} placeholder="Will be sent to staff member" />
        <div>
          <label className="block text-sm font-medium mb-1.5">Role</label>
          <select value={newStaff.role} onChange={(e) => setNewStaff({ ...newStaff, role: e.target.value })} className={INPUT_CLASS}>
            <option value="manager">Manager</option>
            <option value="receptionist">Receptionist</option>
          </select>
        </div>
      </div>
    </Modal>
  );
}

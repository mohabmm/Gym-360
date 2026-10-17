import { Modal } from '@gym360/ui';

const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

interface RemoveMemberModalProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  pending: boolean;
  memberName: string;
}

export function RemoveMemberModal({ open, onClose, onConfirm, pending, memberName }: RemoveMemberModalProps) {
  return (
    <Modal
      open={open}
      onClose={() => !pending && onClose()}
      title="Remove Member"
      size="sm"
      footer={
        <>
          <button
            onClick={onClose}
            disabled={pending}
            className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            disabled={pending}
            className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
          >
            {pending ? 'Removing…' : 'Remove'}
          </button>
        </>
      }
    >
      <p className="text-sm">
        Remove <span className="font-medium">{memberName}</span> from this gym? Active memberships are terminated. This cannot be undone.
      </p>
    </Modal>
  );
}

export interface EditMemberForm {
  name: string;
  nameAr: string;
  email: string;
  phone: string;
}

interface EditMemberModalProps {
  open: boolean;
  onClose: () => void;
  onSave: () => void;
  saving: boolean;
  form: EditMemberForm;
  onChange: (form: EditMemberForm) => void;
}

export function EditMemberModal({ open, onClose, onSave, saving, form, onChange }: EditMemberModalProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Edit Profile"
      size="md"
      footer={
        <>
          <button onClick={onClose} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
          <button onClick={onSave} disabled={saving} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50">
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Name (English)</label>
            <input value={form.name} onChange={(e) => onChange({ ...form, name: e.target.value })} className={INPUT_CLASS} />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Name (Arabic)</label>
            <input value={form.nameAr} onChange={(e) => onChange({ ...form, nameAr: e.target.value })} dir="rtl" className={INPUT_CLASS} />
          </div>
        </div>
        <div>
          <label className="block text-sm font-medium mb-1.5">Email</label>
          <input type="email" value={form.email} onChange={(e) => onChange({ ...form, email: e.target.value })} className={INPUT_CLASS} />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1.5">Phone</label>
          <input type="tel" value={form.phone} onChange={(e) => onChange({ ...form, phone: e.target.value })} className={INPUT_CLASS} />
        </div>
      </div>
    </Modal>
  );
}

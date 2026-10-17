import type { Dispatch, SetStateAction } from 'react';
import { Modal } from '@gym360/ui';
import type { PlanFormState } from '@/lib/plans';
import { CheckboxField, ModalFooter, NumberField, TextField, fieldSetter } from '@/components/plans/form-controls';

interface PlanFormModalBaseProps {
  open: boolean;
  onClose: () => void;
  busy: boolean;
  form: PlanFormState;
  setForm: Dispatch<SetStateAction<PlanFormState>>;
}

interface EditSubscriptionPlanModalProps extends PlanFormModalBaseProps {
  planName: string | undefined;
  onSave: () => void;
  features: { id: string; name: string }[];
}

export function EditSubscriptionPlanModal({ open, onClose, busy, form, setForm, planName, onSave, features }: EditSubscriptionPlanModalProps) {
  const set = fieldSetter<PlanFormState>(setForm);
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Edit ${planName ?? ''} Plan`}
      size="lg"
      footer={<ModalFooter onCancel={onClose} onConfirm={onSave} disabled={busy} label={busy ? 'Saving...' : 'Save Changes'} />}
    >
      {planName !== undefined && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <TextField label="Plan Name" value={form.name} onChange={set('name')} />
            <NumberField label="Monthly Price (EGP)" value={form.monthlyPriceEgp} onChange={set('monthlyPriceEgp')} />
            <NumberField label="Max Members" value={form.memberLimit} onChange={set('memberLimit')} />
            <NumberField label="Max Trainers" value={form.trainerLimit} onChange={set('trainerLimit')} />
            <NumberField label="Max Staff" value={form.staffLimit} onChange={set('staffLimit')} />
            <NumberField label="Sort Order" value={form.sortOrder} onChange={set('sortOrder')} />
          </div>
          <div className="flex items-center gap-4">
            <CheckboxField id="visible" label="Visible to customers" checked={form.isPublic} onChange={set('isPublic')} />
            <CheckboxField id="active" label="Active" checked={form.isActive} onChange={set('isActive')} />
          </div>
          <div>
            <p className="text-xs text-muted-foreground border border-amber-200 bg-amber-50 rounded-lg px-3 py-2 dark:border-amber-800 dark:bg-amber-950/20">
              Feature assignments are managed via the <strong>Plans &amp; Features</strong> page using the Feature Matrix.
            </p>
            <div className="mt-2 grid grid-cols-2 gap-1 max-h-48 overflow-y-auto text-sm text-muted-foreground">
              {features.map((f) => <span key={f.id} className="truncate">{f.name}</span>)}
            </div>
          </div>
        </div>
      )}
    </Modal>
  );
}

interface CreateSubscriptionPlanModalProps extends PlanFormModalBaseProps {
  canCreate: boolean;
  onCreate: () => void;
}

export function CreateSubscriptionPlanModal({ open, onClose, busy, form, setForm, canCreate, onCreate }: CreateSubscriptionPlanModalProps) {
  const set = fieldSetter<PlanFormState>(setForm);
  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Create Plan"
      size="lg"
      footer={
        <ModalFooter onCancel={onClose} onConfirm={onCreate} disabled={busy || !canCreate} label={busy ? 'Creating...' : 'Create Plan'} />
      }
    >
      <div className="grid grid-cols-2 gap-4">
        <TextField label="Plan Name" value={form.name} onChange={set('name')} />
        <TextField label="Plan Code" value={form.code} placeholder="e.g. pro" onChange={set('code')} />
        <NumberField label="Monthly Price (EGP)" value={form.monthlyPriceEgp} onChange={set('monthlyPriceEgp')} />
        <NumberField label="Annual Price (EGP)" value={form.annualPriceEgp} onChange={set('annualPriceEgp')} />
        <NumberField label="Max Members" value={form.memberLimit} onChange={set('memberLimit')} />
        <NumberField label="Max Trainers" value={form.trainerLimit} onChange={set('trainerLimit')} />
        <NumberField label="Max Staff" value={form.staffLimit} onChange={set('staffLimit')} />
        <NumberField label="Sort Order" value={form.sortOrder} onChange={set('sortOrder')} />
        <CheckboxField id="create-visible" label="Visible to customers" checked={form.isPublic} onChange={set('isPublic')} className="flex items-center gap-2 pt-6" />
      </div>
    </Modal>
  );
}

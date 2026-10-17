import type { Dispatch, SetStateAction } from 'react';
import { Modal } from '@gym360/ui';
import { FEATURE_CATEGORIES, type PlanFormState } from '@/lib/plans';
import { CheckboxField, Field, INPUT_CLASS, ModalFooter, NumberField, TextField, fieldSetter } from './form-controls';

export type FeatureForm = { name: string; code: string; category: string; isActive: boolean };
export type CreateFeatureForm = { name: string; code: string; category: string };

function savingLabel(busy: boolean, busyLabel: string, idleLabel: string): string {
  return busy ? busyLabel : idleLabel;
}

function planSaveLabel(busy: boolean, isEditing: boolean): string {
  return savingLabel(busy, 'Saving...', isEditing ? 'Save Changes' : 'Create Plan');
}

function isPlanSaveDisabled(busy: boolean, form: PlanFormState, isEditing: boolean): boolean {
  const missingCode = !isEditing && !form.code?.trim();
  return busy || !form.name?.trim() || missingCode;
}

interface PlanFormModalProps {
  open: boolean;
  onClose: () => void;
  onSave: () => void;
  busy: boolean;
  isEditing: boolean;
  form: PlanFormState;
  setForm: Dispatch<SetStateAction<PlanFormState>>;
}

/** Create/Edit Plan Modal */
export function PlanFormModal({ open, onClose, onSave, busy, isEditing, form, setForm }: PlanFormModalProps) {
  const set = fieldSetter<PlanFormState>(setForm);
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEditing ? 'Edit Plan' : 'Create Plan'}
      size="lg"
      footer={
        <ModalFooter
          onCancel={onClose}
          onConfirm={onSave}
          disabled={isPlanSaveDisabled(busy, form, isEditing)}
          label={planSaveLabel(busy, isEditing)}
        />
      }
    >
      <div className="grid grid-cols-2 gap-4">
        <TextField label="Plan Name" value={form.name} onChange={set('name')} />
        <TextField
          label="Plan Code"
          value={form.code}
          disabled={isEditing}
          placeholder="e.g. pro"
          onChange={set('code')}
          inputClassName={`${INPUT_CLASS} disabled:opacity-60`}
        />
        <TextField label="Description" className="col-span-2" value={form.description} onChange={set('description')} />
        <NumberField label="Monthly Price (EGP)" value={form.monthlyPriceEgp} onChange={set('monthlyPriceEgp')} />
        <NumberField label="Annual Price (EGP)" value={form.annualPriceEgp} onChange={set('annualPriceEgp')} />
        <NumberField label="Member Limit" value={form.memberLimit} onChange={set('memberLimit')} />
        <NumberField label="Trainer Limit" value={form.trainerLimit} onChange={set('trainerLimit')} />
        <NumberField label="Staff Limit" value={form.staffLimit} onChange={set('staffLimit')} />
        <NumberField label="Sort Order" value={form.sortOrder} onChange={set('sortOrder')} />
        <CheckboxField id="plan-public" label="Visible to customers" checked={form.isPublic} onChange={set('isPublic')} className="flex items-center gap-2 pt-6" />
        {isEditing && (
          <CheckboxField id="plan-active" label="Active" checked={form.isActive} onChange={set('isActive')} className="flex items-center gap-2 pt-6" />
        )}
      </div>
    </Modal>
  );
}

function CategorySelect({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  return (
    <Field label="Category">
      <select value={value} onChange={(e) => onChange(e.target.value)} className={INPUT_CLASS}>
        {FEATURE_CATEGORIES.map((category) => <option key={category}>{category}</option>)}
      </select>
    </Field>
  );
}

interface EditFeatureModalProps {
  open: boolean;
  onClose: () => void;
  onSave: () => void;
  busy: boolean;
  form: FeatureForm;
  setForm: Dispatch<SetStateAction<FeatureForm>>;
}

export function EditFeatureModal({ open, onClose, onSave, busy, form, setForm }: EditFeatureModalProps) {
  const set = fieldSetter<FeatureForm>(setForm);
  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Edit Feature"
      size="md"
      footer={<ModalFooter onCancel={onClose} onConfirm={onSave} disabled={busy} label={savingLabel(busy, 'Saving...', 'Save Changes')} />}
    >
      <div className="space-y-4">
        <TextField label="Feature Name" value={form.name} onChange={set('name')} />
        <Field label="Feature Code">
          <input
            type="text"
            value={form.code}
            readOnly
            title="Feature code is immutable after creation"
            className="w-full h-9 rounded-lg border border-input bg-muted px-3 text-sm text-muted-foreground cursor-not-allowed"
          />
          <p className="text-xs text-muted-foreground mt-1">Code cannot be changed after creation.</p>
        </Field>
        <CategorySelect value={form.category} onChange={set('category')} />
        <CheckboxField id="feature-active" label="Active" checked={form.isActive} onChange={set('isActive')} />
      </div>
    </Modal>
  );
}

interface CreateFeatureModalProps {
  open: boolean;
  onClose: () => void;
  onCreate: () => void;
  busy: boolean;
  canCreate: boolean;
  form: CreateFeatureForm;
  setForm: Dispatch<SetStateAction<CreateFeatureForm>>;
}

/** Add Feature Modal */
export function CreateFeatureModal({ open, onClose, onCreate, busy, canCreate, form, setForm }: CreateFeatureModalProps) {
  const set = fieldSetter<CreateFeatureForm>(setForm);
  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Add Feature"
      size="md"
      footer={
        <ModalFooter
          onCancel={onClose}
          onConfirm={onCreate}
          disabled={busy || !canCreate}
          label={savingLabel(busy, 'Creating...', 'Create Feature')}
        />
      }
    >
      <div className="space-y-4">
        <TextField label="Feature Name" value={form.name} onChange={set('name')} placeholder="e.g. Remote Training" />
        <TextField label="Feature Code" value={form.code} onChange={set('code')} placeholder="e.g. remote_training" />
        <CategorySelect value={form.category} onChange={set('category')} />
      </div>
    </Modal>
  );
}

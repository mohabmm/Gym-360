import type { ReactNode } from 'react';

export const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

interface FieldProps {
  label: string;
  className?: string;
  children: ReactNode;
}

export function Field({ label, className, children }: FieldProps) {
  return (
    <div className={className}>
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      {children}
    </div>
  );
}

interface TextFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  inputClassName?: string;
}

export function TextField({ label, value, onChange, placeholder, disabled, className, inputClassName = INPUT_CLASS }: TextFieldProps) {
  return (
    <Field label={label} className={className}>
      <input
        type="text"
        value={value}
        disabled={disabled}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className={inputClassName}
      />
    </Field>
  );
}

interface NumberFieldProps {
  label: string;
  value: number;
  onChange: (value: number) => void;
}

export function NumberField({ label, value, onChange }: NumberFieldProps) {
  return (
    <Field label={label}>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className={INPUT_CLASS}
      />
    </Field>
  );
}

interface CheckboxFieldProps {
  id: string;
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  className?: string;
}

export function CheckboxField({ id, label, checked, onChange, className = 'flex items-center gap-2' }: CheckboxFieldProps) {
  return (
    <div className={className}>
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="rounded border-input"
      />
      <label htmlFor={id} className="text-sm">{label}</label>
    </div>
  );
}

interface ModalFooterProps {
  onCancel: () => void;
  onConfirm: () => void;
  disabled: boolean;
  label: string;
}

export function ModalFooter({ onCancel, onConfirm, disabled, label }: ModalFooterProps) {
  return (
    <>
      <button onClick={onCancel} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
      <button
        onClick={onConfirm}
        disabled={disabled}
        className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
      >
        {label}
      </button>
    </>
  );
}

/** Returns a setter that patches a single key of a form object held in React state. */
export function fieldSetter<T>(setForm: (update: (prev: T) => T) => void) {
  return <K extends keyof T>(key: K) =>
    (value: T[K]) => setForm((prev) => ({ ...prev, [key]: value }));
}

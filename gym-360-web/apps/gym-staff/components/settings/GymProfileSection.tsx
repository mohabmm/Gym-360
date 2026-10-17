import { Check, MapPin } from 'lucide-react';
import { Field, INPUT_CLASS, SettingsCard } from './SettingsCard';

export interface GymProfileForm {
  name: string;
  description: string;
  phone: string;
  altPhones: string;
  email: string;
  website: string;
  street: string;
  city: string;
  area: string;
}

export const EMPTY_PROFILE: GymProfileForm = {
  name: '',
  description: '',
  phone: '',
  altPhones: '',
  email: '',
  website: '',
  street: '',
  city: '',
  area: '',
};

const HALF_INPUT_CLASS = INPUT_CLASS.replace('w-full', 'flex-1');

interface Props {
  profile: GymProfileForm;
  onChange: (field: keyof GymProfileForm, value: string) => void;
  onSave: () => void;
}

export function GymProfileSection({ profile, onChange, onSave }: Props) {
  const input = (field: keyof GymProfileForm, extra: { placeholder?: string; className?: string } = {}) => (
    <input
      value={profile[field]}
      onChange={(e) => onChange(field, e.target.value)}
      placeholder={extra.placeholder}
      className={extra.className ?? INPUT_CLASS}
    />
  );

  return (
    <SettingsCard title="Gym Profile">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Field label="Gym Name">{input('name')}</Field>
        <Field label="Phone">{input('phone')}</Field>
        <Field label="Alt Phones">{input('altPhones')}</Field>
        <Field label="Email">{input('email')}</Field>
        <Field label="Website">{input('website', { placeholder: 'https://firstgym.eg' })}</Field>
        <Field label="Address">{input('street')}</Field>
        <Field label="City / Area">
          <div className="flex gap-2">
            {input('city', { className: HALF_INPUT_CLASS })}
            {input('area', { className: HALF_INPUT_CLASS })}
          </div>
        </Field>
        <Field label="Description" className="md:col-span-2">
          <textarea
            value={profile.description}
            onChange={(e) => onChange('description', e.target.value)}
            rows={3}
            className="w-full px-3 py-2 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 resize-none"
          />
        </Field>
      </div>

      {/* Map placeholder */}
      <div className="mt-4">
        <label className="block text-sm font-medium mb-1.5">Location</label>
        <div className="h-40 rounded-xl border-2 border-dashed border-border bg-muted/30 flex flex-col items-center justify-center text-muted-foreground">
          <MapPin size={28} className="mb-2 text-primary/50" />
          <p className="text-sm font-medium">{profile.street}</p>
          <p className="text-xs">{profile.city}, {profile.area}</p>
          <p className="text-xs mt-2 text-muted-foreground/60">Map integration available in production</p>
        </div>
      </div>

      <button
        onClick={onSave}
        className="mt-4 inline-flex items-center gap-2 h-10 px-5 rounded-xl bg-primary text-primary-foreground font-medium text-sm hover:bg-primary/90 transition-colors"
      >
        <Check size={16} /> Save Changes
      </button>
    </SettingsCard>
  );
}

import { useRef, useState } from 'react';
import { X, Save } from 'lucide-react';

// --- Edit Profile Modal ---
export function EditProfileModalForm({
  open,
  onClose,
  initialBio,
  initialSpecializations,
  onSave,
  isSaving,
}: {
  open: boolean;
  onClose: () => void;
  initialBio: string;
  initialSpecializations: string[];
  onSave: (payload: { bio: string; specializations: string[] }) => void;
  isSaving: boolean;
}) {
  const [bio, setBio] = useState(initialBio);
  const [specsCsv, setSpecsCsv] = useState(initialSpecializations.join(', '));

  // reset when reopened
  const lastOpenRef = useRef(false);
  if (open && !lastOpenRef.current) {
    lastOpenRef.current = true;
    setBio(initialBio);
    setSpecsCsv(initialSpecializations.join(', '));
  } else if (!open && lastOpenRef.current) {
    lastOpenRef.current = false;
  }

  if (!open) return null;

  const specializations = specsCsv
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-md rounded-xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-heading font-semibold text-lg">Edit Trainer Profile</h3>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="space-y-3">
          <label className="block">
            <span className="text-sm font-medium">Specializations</span>
            <input
              value={specsCsv}
              onChange={(e) => setSpecsCsv(e.target.value)}
              placeholder="e.g. Strength, HIIT, Mobility"
              className="mt-1 w-full h-9 rounded-lg border border-border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
            <span className="text-xs text-muted-foreground mt-1 block">Comma-separated.</span>
          </label>
          <label className="block">
            <span className="text-sm font-medium">Bio</span>
            <textarea
              value={bio}
              onChange={(e) => setBio(e.target.value)}
              rows={4}
              placeholder="Short trainer bio…"
              className="mt-1 w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </label>
        </div>
        <div className="flex gap-3 justify-end mt-5">
          <button
            onClick={onClose}
            className="rounded-lg border border-border px-4 py-2 text-sm font-medium hover:bg-muted transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={() => onSave({ bio, specializations })}
            disabled={isSaving}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50 transition-colors"
          >
            <Save className="h-4 w-4" />
            {isSaving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      </div>
    </div>
  );
}

// --- Edit Rate Modal ---
export function EditRateModal({
  currentRate,
  onClose,
  onSave,
  isSaving,
}: {
  currentRate: number | null;
  onClose: () => void;
  onSave: (rate: number) => void;
  isSaving: boolean;
}) {
  const [value, setValue] = useState(currentRate?.toString() ?? '');
  const parsed = parseFloat(value);
  const isValid = !isNaN(parsed) && parsed >= 0 && parsed <= 10000;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-sm rounded-xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-heading font-semibold text-lg">Edit Session Rate</h3>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>
        <p className="text-sm text-muted-foreground mb-4">
          Set the flat per-session rate for this trainer in EGP.
        </p>
        <div className="relative mb-4">
          <input
            type="number"
            min={0}
            max={10000}
            step={1}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="e.g. 150"
            className="w-full rounded-lg border border-border bg-background px-4 py-2.5 pr-14 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
          />
          <span className="absolute right-4 top-1/2 -translate-y-1/2 text-sm font-medium text-muted-foreground">
            EGP
          </span>
        </div>
        {value !== '' && !isValid && (
          <p className="text-xs text-destructive mb-3">Rate must be between 0 and 10,000 EGP.</p>
        )}
        <div className="flex gap-3 justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={() => isValid && onSave(parsed)}
            disabled={!isValid || isSaving}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
          >
            <Save className="h-4 w-4" />
            {isSaving ? 'Saving…' : 'Save Rate'}
          </button>
        </div>
      </div>
    </div>
  );
}

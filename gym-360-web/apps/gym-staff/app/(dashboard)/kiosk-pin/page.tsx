'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { PageHeader, ConfirmModal } from '@gymapp/ui';
import { Lock, AlertTriangle, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { apiClient } from '@/lib/api-client';

type KioskPinInfo = { isSet: boolean; hash: string | null };

const PIN_RE = /^\d{4,8}$/;

export default function KioskPinPage() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;

  const [loading, setLoading] = useState(true);
  const [isSet, setIsSet] = useState(false);
  const [saving, setSaving] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirmPin, setConfirmPin] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [showRemoveConfirm, setShowRemoveConfirm] = useState(false);

  useEffect(() => {
    if (!gymId) return;
    (async () => {
      try {
        const res = await apiClient.get(`/gyms/${gymId}/kiosk-pin`);
        const info = (res.data?.data ?? res.data) as KioskPinInfo;
        setIsSet(info.isSet);
      } catch (e: any) {
        toast.error(e.response?.data?.error?.message ?? 'Failed to load kiosk PIN');
      } finally {
        setLoading(false);
      }
    })();
  }, [gymId]);

  const resetForm = () => {
    setCurrent('');
    setNext('');
    setConfirmPin('');
    setError(null);
  };

  const handleSave = async () => {
    setError(null);
    if (!PIN_RE.test(next)) {
      setError('PIN must be 4–8 digits.');
      return;
    }
    if (next !== confirmPin) {
      setError('New PIN and confirmation do not match.');
      return;
    }
    if (isSet && !PIN_RE.test(current)) {
      setError('Enter the current PIN.');
      return;
    }
    setSaving(true);
    try {
      await apiClient.put(`/gyms/${gymId}/kiosk-pin`, {
        newPin: next,
        currentPin: isSet ? current : undefined,
      });
      setIsSet(true);
      resetForm();
      toast.success('Kiosk PIN saved');
    } catch (e: any) {
      setError(e.response?.data?.error?.message ?? 'Failed to save PIN');
    } finally {
      setSaving(false);
    }
  };

  const handleRemove = async () => {
    setError(null);
    if (!PIN_RE.test(current)) {
      setError('Enter the current PIN to remove it.');
      return;
    }
    setShowRemoveConfirm(true);
  };

  const confirmRemove = async () => {
    setShowRemoveConfirm(false);
    setRemoving(true);
    try {
      await apiClient.delete(`/gyms/${gymId}/kiosk-pin`, {
        data: { currentPin: current },
      });
      setIsSet(false);
      resetForm();
      toast.success('Kiosk PIN removed');
    } catch (e: any) {
      setError(e.response?.data?.error?.message ?? 'Failed to remove PIN');
    } finally {
      setRemoving(false);
    }
  };

  if (!gymId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Kiosk PIN" description="Protect kiosk mode with a PIN." />
        <p className="text-sm text-muted-foreground">Select a gym first.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Kiosk PIN"
        description="Require a PIN to exit kiosk mode on the staff tablet."
      />

      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <div className="flex items-start gap-3">
          <Lock size={18} className="mt-0.5 text-primary" />
          <div>
            {loading ? (
              <p className="text-sm text-muted-foreground">Loading…</p>
            ) : isSet ? (
              <p className="text-sm">A PIN is set. It will be required to exit kiosk mode.</p>
            ) : (
              <div className="flex items-start gap-2">
                <AlertTriangle size={16} className="mt-0.5 text-amber-500" />
                <p className="text-sm">
                  No PIN is set. Kiosk mode currently exits without prompting — set a PIN to protect the device.
                </p>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold text-base mb-4">
          {isSet ? 'Change PIN' : 'Set PIN'}
        </h3>

        <div className="space-y-4 max-w-md">
          {isSet && (
            <div>
              <label className="block text-sm font-medium mb-1.5">Current PIN</label>
              <input
                type="password"
                inputMode="numeric"
                maxLength={8}
                value={current}
                onChange={(e) => setCurrent(e.target.value.replace(/\D/g, ''))}
                className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
              />
            </div>
          )}
          <div>
            <label className="block text-sm font-medium mb-1.5">New PIN (4–8 digits)</label>
            <input
              type="password"
              inputMode="numeric"
              maxLength={8}
              value={next}
              onChange={(e) => setNext(e.target.value.replace(/\D/g, ''))}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Confirm new PIN</label>
            <input
              type="password"
              inputMode="numeric"
              maxLength={8}
              value={confirmPin}
              onChange={(e) => setConfirmPin(e.target.value.replace(/\D/g, ''))}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <button
            type="button"
            disabled={saving || loading || !next || !PIN_RE.test(next) || next !== confirmPin}
            onClick={handleSave}
            className="inline-flex items-center gap-2 h-10 px-5 rounded-xl bg-primary text-primary-foreground font-medium text-sm hover:bg-primary/90 transition-colors disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </div>

      {isSet && (
        <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-6 shadow-sm">
          <h3 className="font-heading font-semibold text-base mb-2 flex items-center gap-2 text-destructive">
            <Trash2 size={16} /> Remove PIN
          </h3>
          <p className="text-sm text-muted-foreground mb-4">
            Enter the current PIN above, then click Remove. Kiosk mode will exit without prompting afterward.
          </p>
          <button
            type="button"
            disabled={removing || !current}
            onClick={handleRemove}
            className="inline-flex items-center gap-2 h-10 px-5 rounded-xl border border-destructive text-destructive font-medium text-sm hover:bg-destructive/10 transition-colors disabled:opacity-50"
          >
            {removing ? 'Removing…' : 'Remove PIN'}
          </button>
        </div>
      )}

      <ConfirmModal
        open={showRemoveConfirm}
        onClose={() => setShowRemoveConfirm(false)}
        onConfirm={confirmRemove}
        title="Remove Kiosk PIN"
        description="Remove the kiosk PIN? Kiosk mode will exit without prompting."
        confirmLabel="Remove"
        variant="danger"
      />
    </div>
  );
}

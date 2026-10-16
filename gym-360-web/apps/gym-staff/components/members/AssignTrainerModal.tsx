import { useState } from 'react';
import { toast } from 'sonner';
import { Modal } from '@gym360/ui';
import type { TrainerListItem } from '@gym360/ui';
import { useQuickAssignTrainer } from '@/lib/hooks/useMembers';

const SESSION_COUNT_OPTIONS = [4, 8, 12, 16, 20, 24, 28, 32, 36, 40];

const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

interface AssignForm {
  trainerId: string;
  sessionCount: number;
  totalPrice: string;
  paymentMethod: string;
}

const EMPTY_FORM: AssignForm = { trainerId: '', sessionCount: 4, totalPrice: '', paymentMethod: 'Cash' };

// Blank price means a free assignment (undefined); otherwise the parsed number.
function parsePrice(raw: string): number | undefined {
  return raw.trim() === '' ? undefined : Number(raw);
}

const isInvalidPrice = (price: number | undefined) =>
  price !== undefined && (Number.isNaN(price) || price < 0);

const isAssignable = (t: TrainerListItem) =>
  (t.status as string) !== 'pending_approval' && (t.status as string) !== 'rejected';

function trainerOptionLabel(t: TrainerListItem) {
  return t.specializations?.length ? ` — ${t.specializations.join(', ')}` : '';
}

interface Props {
  open: boolean;
  onClose: () => void;
  memberId: string;
  trainers: TrainerListItem[];
}

export function AssignTrainerModal({ open, onClose, memberId, trainers }: Props) {
  const quickAssign = useQuickAssignTrainer();
  const [form, setForm] = useState<AssignForm>(EMPTY_FORM);
  const set = (patch: Partial<AssignForm>) => setForm({ ...form, ...patch });

  const handleConfirm = () => {
    if (!form.trainerId) {
      toast.error('Pick a trainer first.');
      return;
    }
    const priceNum = parsePrice(form.totalPrice);
    if (isInvalidPrice(priceNum)) {
      toast.error('Price must be a non-negative number.');
      return;
    }
    quickAssign.mutate(
      {
        memberId,
        trainerId: form.trainerId,
        sessionCount: form.sessionCount,
        totalPrice: priceNum,
        // Only attach a payment row when there's an actual price; free
        // assignments stay package-only.
        paymentMethod: priceNum && priceNum > 0 ? form.paymentMethod : undefined,
      },
      {
        onSuccess: () => {
          toast.success(`Assigned ${form.sessionCount} sessions.`);
          onClose();
          setForm(EMPTY_FORM);
        },
        onError: (e: unknown) => {
          const msg = e instanceof Error ? e.message : 'Failed to assign trainer.';
          toast.error(msg);
        },
      },
    );
  };

  return (
    <Modal
      open={open}
      onClose={() => !quickAssign.isPending && onClose()}
      title="Assign Trainer"
      size="md"
      footer={
        <>
          <button
            onClick={onClose}
            disabled={quickAssign.isPending}
            className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={quickAssign.isPending || !form.trainerId}
            className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
          >
            {quickAssign.isPending ? 'Assigning…' : 'Assign'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1.5">Trainer</label>
          <select
            value={form.trainerId}
            onChange={(e) => set({ trainerId: e.target.value })}
            className={INPUT_CLASS}
          >
            <option value="">Select a trainer…</option>
            {trainers.filter(isAssignable).map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
                {trainerOptionLabel(t)}
              </option>
            ))}
          </select>
          {trainers.length === 0 && (
            <p className="text-xs text-muted-foreground mt-1">No trainers approved at this gym yet.</p>
          )}
        </div>
        <div>
          <label className="block text-sm font-medium mb-1.5">Sessions</label>
          <select
            value={form.sessionCount}
            onChange={(e) => set({ sessionCount: Number(e.target.value) })}
            className={INPUT_CLASS}
          >
            {SESSION_COUNT_OPTIONS.map((n) => (
              <option key={n} value={n}>{n} sessions</option>
            ))}
          </select>
          <p className="text-xs text-muted-foreground mt-1">Multiples of 4.</p>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Total price (EGP)</label>
            <input
              type="number"
              min="0"
              step="0.01"
              placeholder="0 (no payment)"
              value={form.totalPrice}
              onChange={(e) => set({ totalPrice: e.target.value })}
              className={INPUT_CLASS}
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Payment method</label>
            <select
              value={form.paymentMethod}
              onChange={(e) => set({ paymentMethod: e.target.value })}
              disabled={!form.totalPrice || Number(form.totalPrice) === 0}
              className={`${INPUT_CLASS} disabled:opacity-50`}
            >
              <option value="Cash">Cash</option>
              <option value="Card">Card</option>
              <option value="BankTransfer">Bank Transfer</option>
              <option value="MobileWallet">Mobile Wallet</option>
            </select>
          </div>
        </div>
        <p className="text-xs text-muted-foreground">
          Total package price for all sessions. Leave blank for a free assignment. Setting a price records a Payment in the same transaction.
        </p>
      </div>
    </Modal>
  );
}

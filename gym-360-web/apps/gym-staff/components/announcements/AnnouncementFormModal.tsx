import { Modal } from '@gym360/ui';
import type { Announcement } from '@gym360/ui';

export type Draft = Omit<Announcement, 'id' | 'publishedAt' | 'createdBy' | 'createdByName'> & {
  id?: string;
};

export function blankDraft(): Draft {
  return {
    gymId: 'g1',
    title: '',
    titleAr: '',
    body: '',
    bodyAr: '',
    targetAudience: 'all',
    isActive: true,
  };
}

const isDraftComplete = (d: Draft) => !!(d.title ?? '').trim() && !!(d.body ?? '').trim();

interface Props {
  open: boolean;
  onClose: () => void;
  draft: Draft;
  onChange: (draft: Draft) => void;
  onSave: (publish: boolean) => void;
  pending: boolean;
}

export function AnnouncementFormModal({ open, onClose, draft, onChange, onSave, pending }: Props) {
  const disabled = !isDraftComplete(draft) || pending;
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={draft.id ? 'Edit Announcement' : 'Create Announcement'}
      size="lg"
      footer={
        <>
          <button onClick={onClose} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
          <button
            onClick={() => onSave(false)}
            disabled={disabled}
            className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
          >
            Save as draft
          </button>
          <button
            onClick={() => onSave(true)}
            disabled={disabled}
            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          >
            {draft.id ? 'Save & Publish' : 'Publish'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="text-sm font-medium">Title (English)</span>
            <input
              type="text"
              value={draft.title}
              onChange={(e) => onChange({ ...draft, title: e.target.value })}
              className="mt-1 w-full h-9 rounded-lg border px-3 text-sm"
              placeholder="e.g., Ramadan Hours"
            />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Title (Arabic)</span>
            <input
              type="text"
              dir="rtl"
              value={draft.titleAr}
              onChange={(e) => onChange({ ...draft, titleAr: e.target.value })}
              className="mt-1 w-full h-9 rounded-lg border px-3 text-sm"
              placeholder="مواعيد رمضان"
            />
          </label>
        </div>
        <label className="block">
          <span className="text-sm font-medium">Body (English)</span>
          <textarea
            rows={3}
            value={draft.body}
            onChange={(e) => onChange({ ...draft, body: e.target.value })}
            className="mt-1 w-full rounded-lg border px-3 py-2 text-sm resize-none"
          />
        </label>
        <label className="block">
          <span className="text-sm font-medium">Body (Arabic)</span>
          <textarea
            rows={3}
            dir="rtl"
            value={draft.bodyAr}
            onChange={(e) => onChange({ ...draft, bodyAr: e.target.value })}
            className="mt-1 w-full rounded-lg border px-3 py-2 text-sm resize-none"
          />
        </label>
        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="text-sm font-medium">Target Audience</span>
            <select
              value={draft.targetAudience}
              onChange={(e) => onChange({ ...draft, targetAudience: e.target.value as Announcement['targetAudience'] })}
              className="mt-1 w-full h-9 rounded-lg border px-3 text-sm"
            >
              <option value="all">All</option>
              <option value="members">Members Only</option>
              <option value="trainers">Trainers Only</option>
              <option value="staff">Staff Only</option>
            </select>
          </label>
          <label className="block">
            <span className="text-sm font-medium">Expires (optional)</span>
            <input
              type="date"
              value={draft.expiresAt?.slice(0, 10) ?? ''}
              onChange={(e) => onChange({ ...draft, expiresAt: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
              className="mt-1 w-full h-9 rounded-lg border px-3 text-sm"
            />
          </label>
        </div>
      </div>
    </Modal>
  );
}

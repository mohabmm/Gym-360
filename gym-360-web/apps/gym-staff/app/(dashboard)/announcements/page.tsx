'use client';

import { useState } from 'react';
import { PageHeader, StatusBadge, Modal, ConfirmModal, formatDate } from '@gymapp/ui';
import type { Announcement } from '@gymapp/ui';
import { Plus, Megaphone, Edit, Trash2, Eye, EyeOff } from 'lucide-react';
import { useAnnouncements, useCreateAnnouncement, useUpdateAnnouncement, useDeleteAnnouncement } from '@/lib/hooks/useAnnouncements';

type Draft = Omit<Announcement, 'id' | 'publishedAt' | 'createdBy' | 'createdByName'> & {
  id?: string;
};

function blankDraft(): Draft {
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

export default function AnnouncementsPage() {
  const { data: serverAnnouncements = [] } = useAnnouncements();
  const createAnn = useCreateAnnouncement();
  const updateAnn = useUpdateAnnouncement();
  const deleteAnn = useDeleteAnnouncement();

  const [modalOpen, setModalOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(blankDraft());
  const [confirmDelete, setConfirmDelete] = useState<Announcement | null>(null);

  function openCreate() {
    setDraft(blankDraft());
    setModalOpen(true);
  }

  function openEdit(a: Announcement) {
    setDraft({
      id: a.id,
      gymId: a.gymId,
      title: a.title,
      titleAr: a.titleAr,
      body: a.body,
      bodyAr: a.bodyAr,
      targetAudience: a.targetAudience,
      targetSection: a.targetSection,
      isActive: a.isActive,
      expiresAt: a.expiresAt,
    });
    setModalOpen(true);
  }

  function save(publish: boolean) {
    if (draft.id) {
      updateAnn.mutate(
        { id: draft.id, ...draft, isActive: publish ? true : draft.isActive },
        { onSuccess: () => setModalOpen(false) },
      );
    } else {
      createAnn.mutate(
        { ...draft, isActive: publish },
        { onSuccess: () => setModalOpen(false) },
      );
    }
  }

  function togglePublish(a: Announcement) {
    updateAnn.mutate({ id: a.id, isActive: !a.isActive });
  }

  function remove(id: string) {
    deleteAnn.mutate(id, { onSuccess: () => setConfirmDelete(null) });
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Announcements"
        description="Communicate with your gym community"
        actions={
          <button
            onClick={openCreate}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" /> Create Announcement
          </button>
        }
      />

      {serverAnnouncements.length === 0 ? (
        <div className="rounded-xl border bg-card p-10 text-center">
          <Megaphone className="mx-auto h-8 w-8 text-muted-foreground" />
          <p className="mt-3 text-sm text-muted-foreground">No announcements yet.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {serverAnnouncements.map((ann) => (
            <div key={ann.id} className="rounded-xl border bg-card p-6 shadow-sm">
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3 flex-1">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary shrink-0">
                    <Megaphone className="h-5 w-5" />
                  </div>
                  <div className="flex-1">
                    <h3 className="font-semibold">{ann.title}</h3>
                    {ann.titleAr && (
                      <p className="font-semibold text-sm mt-0.5" dir="rtl">{ann.titleAr}</p>
                    )}
                    <p className="text-sm text-muted-foreground mt-1">{ann.body}</p>
                    {ann.bodyAr && (
                      <p className="text-sm text-muted-foreground mt-1" dir="rtl">{ann.bodyAr}</p>
                    )}
                    <div className="flex items-center gap-3 mt-3 text-xs text-muted-foreground flex-wrap">
                      <span>By {ann.createdByName}</span>
                      <span>·</span>
                      <span>{formatDate(ann.publishedAt)}</span>
                      <span>·</span>
                      <StatusBadge variant={ann.isActive ? 'active' : 'inactive'} label={ann.isActive ? 'Published' : 'Draft'} />
                      <StatusBadge variant="default" label={ann.targetAudience} />
                      {ann.expiresAt && <span>Expires {formatDate(ann.expiresAt)}</span>}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  <button
                    onClick={() => togglePublish(ann)}
                    className="rounded-md border p-1.5 hover:bg-accent"
                    aria-label={ann.isActive ? 'Unpublish' : 'Publish'}
                    title={ann.isActive ? 'Unpublish' : 'Publish'}
                  >
                    {ann.isActive ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                  </button>
                  <button onClick={() => openEdit(ann)} className="rounded-md border p-1.5 hover:bg-accent" aria-label="Edit">
                    <Edit className="h-3.5 w-3.5" />
                  </button>
                  <button
                    onClick={() => setConfirmDelete(ann)}
                    className="rounded-md border border-red-200 bg-red-50 p-1.5 text-red-600 hover:bg-red-100"
                    aria-label="Delete"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title={draft.id ? 'Edit Announcement' : 'Create Announcement'}
        size="lg"
        footer={
          <>
            <button onClick={() => setModalOpen(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={() => save(false)}
              disabled={!(draft.title ?? '').trim() || !(draft.body ?? '').trim() || createAnn.isPending || updateAnn.isPending}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              Save as draft
            </button>
            <button
              onClick={() => save(true)}
              disabled={!(draft.title ?? '').trim() || !(draft.body ?? '').trim() || createAnn.isPending || updateAnn.isPending}
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
                onChange={(e) => setDraft({ ...draft, title: e.target.value })}
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
                onChange={(e) => setDraft({ ...draft, titleAr: e.target.value })}
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
              onChange={(e) => setDraft({ ...draft, body: e.target.value })}
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm resize-none"
            />
          </label>
          <label className="block">
            <span className="text-sm font-medium">Body (Arabic)</span>
            <textarea
              rows={3}
              dir="rtl"
              value={draft.bodyAr}
              onChange={(e) => setDraft({ ...draft, bodyAr: e.target.value })}
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm resize-none"
            />
          </label>
          <div className="grid grid-cols-2 gap-4">
            <label className="block">
              <span className="text-sm font-medium">Target Audience</span>
              <select
                value={draft.targetAudience}
                onChange={(e) => setDraft({ ...draft, targetAudience: e.target.value as Announcement['targetAudience'] })}
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
                onChange={(e) => setDraft({ ...draft, expiresAt: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
                className="mt-1 w-full h-9 rounded-lg border px-3 text-sm"
              />
            </label>
          </div>
        </div>
      </Modal>

      <ConfirmModal
        open={!!confirmDelete}
        onClose={() => setConfirmDelete(null)}
        onConfirm={() => confirmDelete && remove(confirmDelete.id)}
        title="Delete Announcement"
        description={confirmDelete ? `Delete "${confirmDelete.title}"? This cannot be undone.` : ''}
        confirmLabel="Delete"
        variant="danger"
      />
    </div>
  );
}

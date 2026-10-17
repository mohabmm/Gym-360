'use client';

import { useState } from 'react';
import { PageHeader, ConfirmModal } from '@gym360/ui';
import type { Announcement } from '@gym360/ui';
import { Plus, Megaphone } from 'lucide-react';
import { useAnnouncements, useCreateAnnouncement, useUpdateAnnouncement, useDeleteAnnouncement } from '@/lib/hooks/useAnnouncements';
import { AnnouncementCard } from '@/components/announcements/AnnouncementCard';
import { AnnouncementFormModal, blankDraft, type Draft } from '@/components/announcements/AnnouncementFormModal';

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
            <AnnouncementCard
              key={ann.id}
              ann={ann}
              onTogglePublish={() => togglePublish(ann)}
              onEdit={() => openEdit(ann)}
              onDelete={() => setConfirmDelete(ann)}
            />
          ))}
        </div>
      )}

      <AnnouncementFormModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        draft={draft}
        onChange={setDraft}
        onSave={save}
        pending={createAnn.isPending || updateAnn.isPending}
      />

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

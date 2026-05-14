'use client';

import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import { PageHeader, StatusBadge, Modal, ConfirmModal, formatTime, DAY_NAMES } from '@gymapp/ui';
import { Plus, CalendarDays, Edit, Trash2 } from 'lucide-react';
import { useClasses, useCreateClass, useUpdateClass, useDeleteClass } from '@/lib/hooks/useClasses';
import { useTrainers } from '@/lib/hooks/useTrainers';
import type { GymClass } from '@gymapp/ui';

type Draft = Omit<GymClass, 'id'> & { id?: string };

const SECTIONS: GymClass['section'][] = ['Mens', 'Womens', 'Mixed'];
const COLORS = ['#C41E3A', '#8B5CF6', '#EF4444', '#F59E0B', '#10B981', '#3B82F6', '#EC4899'];

export default function ClassesPage() {
  const { data: classes = [], isLoading } = useClasses();
  const { data: trainers = [] } = useTrainers();
  const createClass = useCreateClass();
  const updateClass = useUpdateClass();
  const deleteClass = useDeleteClass();

  function blankDraft(): Draft {
    return {
      name: '',
      nameAr: '',
      trainerId: trainers[0]?.id ?? '',
      trainerName: trainers[0]?.name ?? '',
      gymId: 'g1',
      section: 'Mixed',
      capacity: 20,
      enrolledCount: 0,
      dayOfWeek: 0,
      startTime: '18:00',
      endTime: '19:00',
      isRecurring: true,
      status: 'active',
      color: COLORS[0],
    };
  }

  const [modalOpen, setModalOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(blankDraft());
  const [confirmDelete, setConfirmDelete] = useState<GymClass | null>(null);

  const list = classes;

  const classesByDay = useMemo(
    () =>
      DAY_NAMES.map((day, index) => ({
        day,
        dayIndex: index,
        classes: list.filter((c) => c.dayOfWeek === index),
      })).filter((d) => d.classes.length > 0),
    [list],
  );

  function openCreate() {
    setDraft(blankDraft());
    setModalOpen(true);
  }

  function openEdit(c: GymClass) {
    setDraft({ ...c });
    setModalOpen(true);
  }

  function save() {
    const payload = {
      name: draft.name,
      nameAr: draft.nameAr,
      description: draft.description,
      trainerId: draft.trainerId,
      section: draft.section,
      capacity: draft.capacity,
      dayOfWeek: draft.dayOfWeek,
      startTime: draft.startTime,
      endTime: draft.endTime,
      isRecurring: draft.isRecurring,
      color: draft.color,
    };
    const onSuccess = () => {
      toast.success(draft.id ? 'Class updated.' : 'Class created.');
      setModalOpen(false);
    };
    const onError = (err: unknown) => {
      const msg = (err as { response?: { data?: { errors?: { message?: string }[] } } })
        ?.response?.data?.errors?.[0]?.message ?? 'Failed to save class.';
      toast.error(msg);
    };
    if (draft.id) {
      updateClass.mutate({ id: draft.id, ...payload }, { onSuccess, onError });
    } else {
      createClass.mutate(payload, { onSuccess, onError });
    }
  }

  function handleDeleteConfirm() {
    if (!confirmDelete) return;
    deleteClass.mutate(confirmDelete.id, {
      onSuccess: () => toast.success('Class deleted.'),
      onError: () => toast.error('Failed to delete class.'),
    });
    setConfirmDelete(null);
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Classes"
        description={`${list.length} weekly classes at First Gym`}
        actions={
          <button
            onClick={openCreate}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" /> Create Class
          </button>
        }
      />

      {isLoading && (
        <div className="rounded-xl border bg-card p-6 text-center text-sm text-muted-foreground">
          Loading classes…
        </div>
      )}

      {!isLoading && classesByDay.length === 0 ? (
        <div className="rounded-xl border bg-card p-10 text-center">
          <p className="text-sm text-muted-foreground">No classes scheduled yet.</p>
          <button
            onClick={openCreate}
            className="mt-4 inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" /> Add your first class
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {classesByDay.map(({ day, classes: dayClasses }) => (
            <div key={day} className="rounded-xl border bg-card shadow-sm overflow-hidden">
              <div className="px-6 py-3 border-b bg-muted/30">
                <h3 className="font-semibold text-sm flex items-center gap-2">
                  <CalendarDays className="h-4 w-4 text-primary" />
                  {day}
                </h3>
              </div>
              <div className="p-4 space-y-3">
                {dayClasses.map((cls) => (
                  <div key={cls.id} className="flex items-center gap-4 py-2 px-3 rounded-lg hover:bg-muted/50 transition-colors">
                    <div className="w-1.5 h-12 rounded-full" style={{ backgroundColor: cls.color ?? '#C41E3A' }} />
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <p className="font-medium">{cls.name}</p>
                        <StatusBadge variant={cls.status === 'full' ? 'warning' : 'active'} label={cls.status} />
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {formatTime(cls.startTime)} - {formatTime(cls.endTime)} · {cls.trainerName} · {cls.section}
                      </p>
                    </div>
                    <div className="text-right mr-2">
                      <p className="text-sm font-medium">{cls.enrolledCount}/{cls.capacity}</p>
                      <div className="w-16 h-1.5 rounded-full bg-muted mt-1">
                        <div
                          className={`h-full rounded-full ${cls.enrolledCount >= cls.capacity ? 'bg-amber-500' : 'bg-emerald-500'}`}
                          style={{ width: `${Math.min((cls.enrolledCount / cls.capacity) * 100, 100)}%` }}
                        />
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      <button
                        onClick={() => openEdit(cls)}
                        className="rounded-md border p-1.5 hover:bg-accent"
                        aria-label="Edit"
                      >
                        <Edit className="h-3.5 w-3.5" />
                      </button>
                      <button
                        onClick={() => setConfirmDelete(cls)}
                        className="rounded-md border border-red-200 bg-red-50 p-1.5 text-red-600 hover:bg-red-100"
                        aria-label="Delete"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title={draft.id ? 'Edit Class' : 'Create Class'}
        size="lg"
        footer={
          <>
            <button onClick={() => setModalOpen(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button
              onClick={save}
              disabled={!draft.name.trim() || !draft.trainerId || createClass.isPending || updateClass.isPending}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {createClass.isPending || updateClass.isPending ? 'Saving…' : 'Save'}
            </button>
          </>
        }
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <label className="block">
              <span className="text-sm font-medium">Name</span>
              <input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" placeholder="e.g., Morning Yoga" />
            </label>
            <div className="block">
              <span className="text-sm font-medium">Arabic</span>
              <input value={draft.nameAr ?? ''} onChange={(e) => setDraft({ ...draft, nameAr: e.target.value })} dir="rtl" className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" placeholder="يوجا صباحية" />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="block">
              <span className="text-sm font-medium">Trainer</span>
              <select value={draft.trainerId} onChange={(e) => setDraft({ ...draft, trainerId: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm">
                {trainers.filter((t) => t.status === 'active').map((t) => (
                  <option key={t.id} value={t.id}>{t.name}</option>
                ))}
              </select>
            </div>
            <div className="block">
              <span className="text-sm font-medium">Section</span>
              <div className="mt-1 flex gap-1">
                {SECTIONS.map((s) => (
                  <button
                    key={s}
                    type="button"
                    onClick={() => setDraft({ ...draft, section: s })}
                    className={`h-9 flex-1 rounded-lg border px-2 text-xs font-medium ${draft.section === s ? 'bg-primary text-white border-primary' : 'hover:bg-muted'}`}
                  >
                    {s}
                  </button>
                ))}
              </div>
            </div>
            <div className="block">
              <span className="text-sm font-medium">Capacity</span>
              <input type="number" min={1} value={draft.capacity} onChange={(e) => setDraft({ ...draft, capacity: Number(e.target.value) })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="block">
              <span className="text-sm font-medium">Day</span>
              <div className="mt-1 grid grid-cols-7 gap-0.5">
                {DAY_NAMES.map((d, i) => (
                  <button
                    key={d}
                    type="button"
                    onClick={() => setDraft({ ...draft, dayOfWeek: i })}
                    className={`h-9 rounded-md border text-[10px] font-medium ${draft.dayOfWeek === i ? 'bg-primary text-white border-primary' : 'hover:bg-muted'}`}
                  >
                    {d.slice(0, 3)}
                  </button>
                ))}
              </div>
            </div>
            <div className="block">
              <span className="text-sm font-medium">Start</span>
              <input type="time" value={draft.startTime} onChange={(e) => setDraft({ ...draft, startTime: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
            </div>
            <div className="block">
              <span className="text-sm font-medium">End</span>
              <input type="time" value={draft.endTime} onChange={(e) => setDraft({ ...draft, endTime: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
            </div>
          </div>

          <div className="flex items-center gap-4">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={draft.isRecurring} onChange={(e) => setDraft({ ...draft, isRecurring: e.target.checked })} />
              Recurring weekly
            </label>
            <div className="flex items-center gap-2">
              <span className="text-sm">Color:</span>
              {COLORS.map((c) => (
                <button
                  key={c}
                  type="button"
                  onClick={() => setDraft({ ...draft, color: c })}
                  className={`h-5 w-5 rounded-full border-2 ${draft.color === c ? 'border-foreground' : 'border-transparent'}`}
                  style={{ backgroundColor: c }}
                  aria-label={c}
                />
              ))}
            </div>
          </div>
        </div>
      </Modal>

      <ConfirmModal
        open={!!confirmDelete}
        onClose={() => setConfirmDelete(null)}
        onConfirm={handleDeleteConfirm}
        title="Delete Class"
        description={confirmDelete ? `Delete "${confirmDelete.name}"? Enrolled members will be notified.` : ''}
        confirmLabel="Delete"
        variant="danger"
      />
    </div>
  );
}

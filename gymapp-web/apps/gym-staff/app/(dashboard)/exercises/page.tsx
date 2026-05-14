'use client';

import { useState, useMemo } from 'react';
import { PageHeader, DataTable, StatusBadge, Modal, FeatureGate, FEATURE_CODES } from '@gymapp/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Filter, Plus, BookOpen } from 'lucide-react';

interface ExerciseRow {
  id: string;
  name: string;
  nameAr: string;
  muscleGroup: string;
  equipment: string;
  difficulty: string;
  isCustom: boolean;
}

const globalExercises: ExerciseRow[] = [
  { id: '1', name: 'Barbell Bench Press', nameAr: 'ضغط بنش بالبار', muscleGroup: 'Chest', equipment: 'Barbell', difficulty: 'Intermediate', isCustom: false },
  { id: '2', name: 'Barbell Squat', nameAr: 'سكوات بالبار', muscleGroup: 'Quads', equipment: 'Barbell', difficulty: 'Intermediate', isCustom: false },
  { id: '3', name: 'Deadlift', nameAr: 'ديد لفت', muscleGroup: 'Back', equipment: 'Barbell', difficulty: 'Advanced', isCustom: false },
  { id: '4', name: 'Pull-ups', nameAr: 'تمرين العقلة', muscleGroup: 'Back', equipment: 'Pull-up Bar', difficulty: 'Intermediate', isCustom: false },
  { id: '5', name: 'Overhead Press', nameAr: 'ضغط علوي', muscleGroup: 'Shoulders', equipment: 'Barbell', difficulty: 'Intermediate', isCustom: false },
  { id: '6', name: 'Lat Pulldown', nameAr: 'سحب عالي', muscleGroup: 'Lats', equipment: 'Cable', difficulty: 'Beginner', isCustom: false },
  { id: '7', name: 'Leg Press', nameAr: 'ليج بريس', muscleGroup: 'Quads', equipment: 'Machine', difficulty: 'Beginner', isCustom: false },
  { id: '8', name: 'Barbell Curl', nameAr: 'كيرل بالبار', muscleGroup: 'Biceps', equipment: 'Barbell', difficulty: 'Beginner', isCustom: false },
  { id: '9', name: 'Tricep Pushdown', nameAr: 'تراي بالكيبل', muscleGroup: 'Triceps', equipment: 'Cable', difficulty: 'Beginner', isCustom: false },
  { id: '10', name: 'Plank', nameAr: 'بلانك', muscleGroup: 'Abs', equipment: 'Bodyweight', difficulty: 'Beginner', isCustom: false },
];

const columns: ColumnDef<ExerciseRow, unknown>[] = [
  {
    accessorKey: 'name',
    header: 'Exercise',
    cell: ({ row }) => (
      <div>
        <p className="font-medium">{row.original.name}</p>
        <p className="text-xs text-muted-foreground">{row.original.nameAr}</p>
      </div>
    ),
  },
  { accessorKey: 'muscleGroup', header: 'Muscle Group' },
  { accessorKey: 'equipment', header: 'Equipment' },
  {
    accessorKey: 'difficulty',
    header: 'Difficulty',
    cell: ({ row }) => (
      <StatusBadge
        variant={row.original.difficulty === 'Beginner' ? 'active' : row.original.difficulty === 'Intermediate' ? 'warning' : 'suspended'}
        label={row.original.difficulty}
      />
    ),
  },
  {
    accessorKey: 'isCustom',
    header: 'Source',
    cell: ({ row }) => <StatusBadge variant={row.original.isCustom ? 'pro' : 'default'} label={row.original.isCustom ? 'Custom' : 'Global'} />,
  },
];

export default function ExercisesPage() {
  const [muscleFilter, setMuscleFilter] = useState('all');
  const [customExercises, setCustomExercises] = useState<ExerciseRow[]>([]);
  const [showAddCustom, setShowAddCustom] = useState(false);
  const [newExercise, setNewExercise] = useState({ name: '', nameAr: '', muscleGroup: 'Chest', equipment: 'Barbell', difficulty: 'Beginner' });
  const [formError, setFormError] = useState<string | null>(null);
  const muscleGroups = useMemo(() => [...new Set(globalExercises.map((e) => e.muscleGroup))].sort(), []);

  const filtered = useMemo(() => {
    if (muscleFilter === 'all') return globalExercises;
    return globalExercises.filter((e) => e.muscleGroup === muscleFilter);
  }, [muscleFilter]);

  const handleAddCustomExercise = () => {
    if (!newExercise.name.trim()) {
      setFormError('Exercise name is required.');
      return;
    }
    setFormError(null);
    setCustomExercises((prev) => [
      ...prev,
      { id: crypto.randomUUID(), ...newExercise, isCustom: true },
    ]);
    setNewExercise({ name: '', nameAr: '', muscleGroup: 'Chest', equipment: 'Barbell', difficulty: 'Beginner' });
    setShowAddCustom(false);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Exercises"
        description="Global exercise library and custom exercises"
      />

      {/* Global Library */}
      <div>
        <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <BookOpen className="h-5 w-5 text-primary" />
          Global Exercise Library
        </h2>
        <div className="flex items-center gap-3 mb-4">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <select value={muscleFilter} onChange={(e) => setMuscleFilter(e.target.value)} className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
            <option value="all">All Muscle Groups</option>
            {muscleGroups.map((g) => <option key={g} value={g}>{g}</option>)}
          </select>
        </div>
        <DataTable data={filtered} columns={columns} searchPlaceholder="Search exercises..." searchColumn="name" />
      </div>

      {/* Custom Exercises - Feature Gated */}
      <FeatureGate featureCode={FEATURE_CODES.CUSTOM_EXERCISE_LIBRARY} requiredPlan="Business">
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold">Custom Exercises</h2>
            <button
              onClick={() => setShowAddCustom(true)}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
            >
              <Plus className="h-4 w-4" /> Add Custom Exercise
            </button>
          </div>
          {customExercises.length === 0 ? (
            <div className="rounded-xl border bg-card p-6 shadow-sm text-center text-muted-foreground">
              No custom exercises yet. Create one to get started.
            </div>
          ) : (
            <DataTable data={customExercises} columns={columns} searchPlaceholder="Search custom exercises..." searchColumn="name" />
          )}
        </div>
      </FeatureGate>

      <Modal
        open={showAddCustom}
        onClose={() => setShowAddCustom(false)}
        title="Add Custom Exercise"
        size="md"
        footer={
          <>
            <button onClick={() => setShowAddCustom(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={handleAddCustomExercise} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">Add Exercise</button>
          </>
        }
      >
        <div className="space-y-3">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <div className="grid grid-cols-2 gap-3">
            <label className="block">
              <span className="text-sm font-medium">Name (English)</span>
              <input value={newExercise.name} onChange={(e) => setNewExercise({ ...newExercise, name: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </label>
            <label className="block">
              <span className="text-sm font-medium">Name (Arabic)</span>
              <input value={newExercise.nameAr} onChange={(e) => setNewExercise({ ...newExercise, nameAr: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" dir="rtl" />
            </label>
          </div>
          <div className="grid grid-cols-3 gap-3">
            <label className="block">
              <span className="text-sm font-medium">Muscle Group</span>
              <select value={newExercise.muscleGroup} onChange={(e) => setNewExercise({ ...newExercise, muscleGroup: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
                {muscleGroups.map((g) => <option key={g} value={g}>{g}</option>)}
              </select>
            </label>
            <label className="block">
              <span className="text-sm font-medium">Equipment</span>
              <input value={newExercise.equipment} onChange={(e) => setNewExercise({ ...newExercise, equipment: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
            </label>
            <label className="block">
              <span className="text-sm font-medium">Difficulty</span>
              <select value={newExercise.difficulty} onChange={(e) => setNewExercise({ ...newExercise, difficulty: e.target.value })} className="mt-1 w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
                <option value="Beginner">Beginner</option>
                <option value="Intermediate">Intermediate</option>
                <option value="Advanced">Advanced</option>
              </select>
            </label>
          </div>
        </div>
      </Modal>
    </div>
  );
}

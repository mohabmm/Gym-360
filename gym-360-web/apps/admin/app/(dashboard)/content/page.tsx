'use client';

import { useState, useMemo } from 'react';
import { PageHeader, DataTable, StatusBadge, useApiQuery, TRAINING } from '@gymapp/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { ExternalLink, Filter, AlertCircle } from 'lucide-react';

interface ExerciseDto {
  id: string;
  name: string;
  nameAr: string | null;
  description: string | null;
  instructions: string | null;
  demoMediaUrl: string | null;
  muscleGroups: string[];
  equipment: string;
  category: string;
  scope: string;
  gymId: string | null;
  createdBy: string | null;
  isActive: boolean;
}

interface ExercisePage {
  items: ExerciseDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface ExerciseRow {
  id: string;
  name: string;
  nameAr: string;
  muscleGroup: string;
  equipment: string;
  category: string;
  demoVideoUrl: string;
  isActive: boolean;
}

export default function ContentPage() {
  const [muscleGroupFilter, setMuscleGroupFilter] = useState('all');

  const { data: exercisesData, isLoading, isError } = useApiQuery<ExercisePage>(
    ['exercises', 'admin'],
    TRAINING.EXERCISES,
    { page: 1, pageSize: 500 },
  );

  const rows: ExerciseRow[] = useMemo(() => {
    const items = exercisesData?.data?.items ?? [];
    return items.map((e) => ({
      id: e.id,
      name: e.name,
      nameAr: e.nameAr ?? '',
      muscleGroup: e.muscleGroups?.[0] ?? '—',
      equipment: e.equipment,
      category: e.category,
      demoVideoUrl: e.demoMediaUrl ?? '',
      isActive: e.isActive,
    }));
  }, [exercisesData]);

  const openDemoVideo = (url: string) => {
    if (!url) return;
    window.open(url, '_blank', 'noopener,noreferrer');
  };

  const columns: ColumnDef<ExerciseRow, unknown>[] = [
    {
      accessorKey: 'name',
      header: 'Exercise',
      cell: ({ row }) => (
        <div>
          <p className="font-medium">{row.original.name}</p>
          {row.original.nameAr && (
            <p className="text-xs text-muted-foreground" dir="rtl">{row.original.nameAr}</p>
          )}
        </div>
      ),
    },
    { accessorKey: 'muscleGroup', header: 'Muscle Group' },
    { accessorKey: 'equipment', header: 'Equipment' },
    { accessorKey: 'category', header: 'Category' },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <StatusBadge
          variant={row.original.isActive ? 'active' : 'inactive'}
          label={row.original.isActive ? 'Active' : 'Inactive'}
        />
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <button
          onClick={() => openDemoVideo(row.original.demoVideoUrl)}
          aria-label={`Open demo for ${row.original.name}`}
          disabled={!row.original.demoVideoUrl}
          className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground disabled:opacity-40 disabled:cursor-not-allowed"
        >
          <ExternalLink className="h-3.5 w-3.5" />
        </button>
      ),
    },
  ];

  const muscleGroups = useMemo(() => [...new Set(rows.map((e) => e.muscleGroup))].filter(Boolean).sort(), [rows]);

  const filtered = useMemo(() => {
    if (muscleGroupFilter === 'all') return rows;
    return rows.filter((e) => e.muscleGroup === muscleGroupFilter);
  }, [rows, muscleGroupFilter]);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Content"
        description={isLoading ? 'Loading exercise library…' : `Exercise library with ${rows.length} exercises`}
      />
      {isError && (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">Failed to load exercise library. Please refresh the page.</p>
        </div>
      )}

      <div className="flex items-center gap-3">
        <Filter className="h-4 w-4 text-muted-foreground" />
        <select
          value={muscleGroupFilter}
          onChange={(e) => setMuscleGroupFilter(e.target.value)}
          className="h-9 rounded-lg border border-input bg-background px-3 text-sm"
        >
          <option value="all">All Muscle Groups</option>
          {muscleGroups.map((group) => (
            <option key={group} value={group}>{group}</option>
          ))}
        </select>
      </div>

      <DataTable data={filtered} columns={columns} searchPlaceholder="Search exercises..." searchColumn="name" enableExport pageSize={15} />
    </div>
  );
}

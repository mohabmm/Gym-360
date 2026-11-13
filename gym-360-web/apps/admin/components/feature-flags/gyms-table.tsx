'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import { Layers } from 'lucide-react';
import { SearchInput, StatusBadge, type FeaturePreset, type GymFlagSummary } from '@gym360/ui';
import { useFlagGyms, useFlagPresets, useSetGymPreset } from '@/lib/hooks';
import { useFlagsText } from '@/lib/feature-flags-text';
import { FLAGS_BASE, useConfirmedAction } from './flags-shell';

/** Selected gym ids, with toggle-one and select-all. */
function useGymSelection(gyms: readonly GymFlagSummary[]) {
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const allSelected = gyms.length > 0 && gyms.every((g) => selected.has(g.gymId));

  const toggle = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  const toggleAll = () => setSelected(allSelected ? new Set() : new Set(gyms.map((g) => g.gymId)));
  const clear = () => setSelected(new Set());

  return { selected, allSelected, toggle, toggleAll, clear };
}

/** Gyms list: name, preset, number of overrides, search, and bulk "apply preset". */
export function GymsTable() {
  const { t, presetName } = useFlagsText();
  const [search, setSearch] = useState('');
  const [presetFilter, setPresetFilter] = useState('');
  const { data, isLoading, isError } = useFlagGyms(search, presetFilter);
  const { data: presetsData } = useFlagPresets();

  const gyms = useMemo(() => data?.data?.items ?? [], [data]);
  const presets: FeaturePreset[] = presetsData?.data ?? [];
  const nameOf = (code: string) => presetName(presets.find((p) => p.code === code), code);
  const selection = useGymSelection(gyms);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <SearchInput value={search} onChange={setSearch} placeholder={t.searchGyms} className="w-72" />
        <PresetSelect value={presetFilter} onChange={setPresetFilter} presets={presets} placeholder={t.allPresets} />
        {selection.selected.size > 0 && (
          <BulkPresetBar gymIds={[...selection.selected]} presets={presets} nameOf={nameOf} onApplied={selection.clear} />
        )}
      </div>

      <div className="overflow-x-auto rounded-xl border border-border bg-card">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-muted-foreground">
            <tr>
              <th className="w-10 px-4 py-3 text-start">
                <input type="checkbox" aria-label={t.applyPreset} checked={selection.allSelected} onChange={selection.toggleAll} />
              </th>
              <th className="px-4 py-3 text-start font-medium">{t.gym}</th>
              <th className="px-4 py-3 text-start font-medium">{t.status}</th>
              <th className="px-4 py-3 text-start font-medium">{t.preset}</th>
              <th className="px-4 py-3 text-start font-medium">{t.overrides}</th>
            </tr>
          </thead>
          <tbody>
            {gyms.map((gym) => (
              <GymRow key={gym.gymId} gym={gym} presetLabel={nameOf(gym.presetCode)}
                checked={selection.selected.has(gym.gymId)} onToggle={() => selection.toggle(gym.gymId)} />
            ))}
          </tbody>
        </table>
        {!isLoading && gyms.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">{isError ? t.loadError : t.empty}</p>
        )}
      </div>
    </div>
  );
}

/** "N selected · preset · Apply preset", with a confirmation dialog. */
function BulkPresetBar({ gymIds, presets, nameOf, onApplied }: {
  gymIds: string[];
  presets: FeaturePreset[];
  nameOf: (code: string) => string;
  onApplied: () => void;
}) {
  const { t } = useFlagsText();
  const [bulkPreset, setBulkPreset] = useState('');
  const setPreset = useSetGymPreset();
  const { ask, dialog } = useConfirmedAction();

  const applyBulk = () => {
    if (!bulkPreset) return;
    ask({
      title: t.confirmPresetTitle,
      description: t.confirmPresetBody(gymIds.length, nameOf(bulkPreset)),
      run: async () => {
        await setPreset.mutateAsync({ gymIds, presetCode: bulkPreset });
        onApplied();
      },
    });
  };

  return (
    <div className="ms-auto flex items-center gap-2 rounded-lg border border-border bg-muted/40 px-3 py-2">
      <span className="text-sm font-medium">{t.selected(gymIds.length)}</span>
      <PresetSelect value={bulkPreset} onChange={setBulkPreset} presets={presets} placeholder={t.choosePreset} />
      <button
        type="button"
        onClick={applyBulk}
        disabled={!bulkPreset}
        className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
      >
        <Layers className="h-4 w-4" /> {t.applyPreset}
      </button>
      {dialog}
    </div>
  );
}

function GymRow({ gym, presetLabel, checked, onToggle }: {
  gym: GymFlagSummary;
  presetLabel: string;
  checked: boolean;
  onToggle: () => void;
}) {
  return (
    <tr className="border-t border-border hover:bg-muted/30">
      <td className="px-4 py-3">
        <input type="checkbox" aria-label={gym.name} checked={checked} onChange={onToggle} />
      </td>
      <td className="px-4 py-3 font-medium">
        <Link href={`${FLAGS_BASE}/gyms/${gym.gymId}`} className="text-primary hover:underline">
          {gym.name}
        </Link>
      </td>
      <td className="px-4 py-3">
        <StatusBadge variant={gym.status.toLowerCase()} label={gym.status} />
      </td>
      <td className="px-4 py-3">{presetLabel}</td>
      <td className="px-4 py-3">{gym.overrideCount}</td>
    </tr>
  );
}

export function PresetSelect({ value, onChange, presets, placeholder, className }: {
  value: string;
  onChange: (code: string) => void;
  presets: FeaturePreset[];
  placeholder?: string;
  className?: string;
}) {
  const { presetName } = useFlagsText();
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className={className ?? 'rounded-md border border-input bg-background px-3 py-2 text-sm'}
    >
      {placeholder !== undefined && <option value="">{placeholder}</option>}
      {presets.map((p) => (
        <option key={p.code} value={p.code}>
          {presetName(p, p.code)}
        </option>
      ))}
    </select>
  );
}

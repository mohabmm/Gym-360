'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import { Layers } from 'lucide-react';
import { SearchInput, StatusBadge, type FeaturePreset, type GymFlagSummary } from '@gym360/ui';
import { useFlagGyms, useFlagPresets, useSetGymPreset } from '@/lib/hooks';
import { useFlagsText } from '@/lib/feature-flags-text';
import { FLAGS_BASE, useConfirmedAction } from './flags-shell';

/** Gyms list: name, preset, number of overrides, search, and bulk "apply preset". */
export function GymsTable() {
  const { t, presetName } = useFlagsText();
  const [search, setSearch] = useState('');
  const [presetFilter, setPresetFilter] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [bulkPreset, setBulkPreset] = useState('');
  const { data, isLoading, isError } = useFlagGyms(search, presetFilter);
  const { data: presetsData } = useFlagPresets();
  const setPreset = useSetGymPreset();
  const { ask, dialog } = useConfirmedAction();

  const gyms = useMemo(() => data?.data?.items ?? [], [data]);
  const presets: FeaturePreset[] = presetsData?.data ?? [];
  const nameOf = (code: string) => presetName(presets.find((p) => p.code === code), code);
  const allSelected = gyms.length > 0 && gyms.every((g) => selected.has(g.gymId));

  const toggle = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const applyBulk = () => {
    if (!bulkPreset || selected.size === 0) return;
    const gymIds = [...selected];
    ask({
      title: t.confirmPresetTitle,
      description: t.confirmPresetBody(gymIds.length, nameOf(bulkPreset)),
      run: async () => {
        await setPreset.mutateAsync({ gymIds, presetCode: bulkPreset });
        setSelected(new Set());
      },
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <SearchInput value={search} onChange={setSearch} placeholder={t.searchGyms} className="w-72" />
        <PresetSelect value={presetFilter} onChange={setPresetFilter} presets={presets} placeholder={t.allPresets} />
        {selected.size > 0 && (
          <div className="ms-auto flex items-center gap-2 rounded-lg border border-border bg-muted/40 px-3 py-2">
            <span className="text-sm font-medium">{t.selected(selected.size)}</span>
            <PresetSelect value={bulkPreset} onChange={setBulkPreset} presets={presets} placeholder={t.choosePreset} />
            <button
              type="button"
              onClick={applyBulk}
              disabled={!bulkPreset}
              className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
            >
              <Layers className="h-4 w-4" /> {t.applyPreset}
            </button>
          </div>
        )}
      </div>

      <div className="overflow-x-auto rounded-xl border border-border bg-card">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-muted-foreground">
            <tr>
              <th className="w-10 px-4 py-3 text-start">
                <input
                  type="checkbox"
                  aria-label={t.applyPreset}
                  checked={allSelected}
                  onChange={() => setSelected(allSelected ? new Set() : new Set(gyms.map((g) => g.gymId)))}
                />
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
                checked={selected.has(gym.gymId)} onToggle={() => toggle(gym.gymId)} />
            ))}
          </tbody>
        </table>
        {!isLoading && gyms.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">{isError ? t.loadError : t.empty}</p>
        )}
      </div>
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

'use client';

import { useMemo } from 'react';
import Link from 'next/link';
import { ArrowLeft, RotateCcw } from 'lucide-react';
import type { GymFlagDetail, GymFlagRow } from '@gym360/ui';
import { useClearOverride, useFlagPresets, useGymFlags, useSetGymPreset, useSetOverride } from '@/lib/hooks';
import { useFlagsText } from '@/lib/feature-flags-text';
import { FLAGS_BASE, OnOffBadge, SourceBadge, Toggle, useConfirmedAction } from './flags-shell';
import { PresetSelect } from './gyms-table';

/** Module order in the dashboard: PT first, then everything else alphabetically. */
function groupByModule(rows: GymFlagRow[]): [string, GymFlagRow[]][] {
  const groups = new Map<string, GymFlagRow[]>();
  for (const row of rows) {
    const list = groups.get(row.feature.module) ?? [];
    list.push(row);
    groups.set(row.feature.module, list);
  }
  return [...groups.entries()].sort(([a], [b]) => (a === 'pt' ? -1 : b === 'pt' ? 1 : a.localeCompare(b)));
}

export function GymFlagsPanel({ gymId }: { gymId: string }) {
  const { t } = useFlagsText();
  const { data, isLoading, isError } = useGymFlags(gymId);
  const detail = data?.data;

  return (
    <div className="space-y-5">
      <Link href={FLAGS_BASE} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="h-4 w-4 rtl:rotate-180" /> {t.backToGyms}
      </Link>
      {isError && <p className="text-sm text-red-600">{t.loadError}</p>}
      {isLoading && <div className="h-40 animate-pulse rounded-xl bg-muted" />}
      {detail && <GymFlagsBody detail={detail} />}
    </div>
  );
}

function GymFlagsBody({ detail }: { detail: GymFlagDetail }) {
  const { t, flagName, presetName } = useFlagsText();
  const { data: presetsData } = useFlagPresets();
  const presets = presetsData?.data ?? [];
  const setPreset = useSetGymPreset();
  const setOverride = useSetOverride();
  const clearOverride = useClearOverride();
  const { ask, dialog } = useConfirmedAction();
  const groups = useMemo(() => groupByModule(detail.flags), [detail.flags]);

  const changePreset = (code: string) => {
    if (!code || code === detail.presetCode) return;
    ask({
      title: t.confirmPresetTitle,
      description: t.confirmPresetBody(1, presetName(presets.find((p) => p.code === code), code)),
      run: () => setPreset.mutateAsync({ gymIds: [detail.gymId], presetCode: code }),
    });
  };

  const toggleFlag = (row: GymFlagRow, enabled: boolean) =>
    ask({
      title: t.confirmOverrideTitle,
      description: t.confirmOverrideBody(flagName(row.feature), enabled, detail.name),
      run: () => setOverride.mutateAsync({ gymId: detail.gymId, key: row.feature.key, enabled }),
    });

  const reset = (row: GymFlagRow) =>
    ask({
      title: t.confirmResetTitle,
      description: t.confirmResetBody(flagName(row.feature)),
      run: () => clearOverride.mutateAsync({ gymId: detail.gymId, key: row.feature.key }),
    });

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 rounded-xl border border-border bg-card p-4">
        <h2 className="text-lg font-semibold">{detail.name}</h2>
        <label className="ms-auto flex items-center gap-2 text-sm">
          <span className="text-muted-foreground">{t.preset}</span>
          <PresetSelect value={detail.presetCode} onChange={changePreset} presets={presets} />
        </label>
      </div>

      {groups.map(([module, rows]) => (
        <section key={module} className="overflow-hidden rounded-xl border border-border bg-card">
          <h3 className="border-b border-border bg-muted/40 px-4 py-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            {module}
          </h3>
          <ul className="divide-y divide-border">
            {rows.map((row) => (
              <FlagRow key={row.feature.key} row={row} onToggle={(on) => toggleFlag(row, on)} onReset={() => reset(row)} />
            ))}
          </ul>
        </section>
      ))}
      {dialog}
    </>
  );
}

function FlagRow({ row, onToggle, onReset }: {
  row: GymFlagRow;
  onToggle: (enabled: boolean) => void;
  onReset: () => void;
}) {
  const { t, flagName, flagDescription } = useFlagsText();
  const name = flagName(row.feature);

  return (
    <li className="flex flex-wrap items-center gap-4 px-4 py-3">
      <div className="min-w-[16rem] flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-medium">{name}</span>
          {row.feature.kind === 'setting' && (
            <span className="rounded bg-purple-100 px-1.5 py-0.5 text-[11px] font-medium text-purple-700 dark:bg-purple-900/30 dark:text-purple-300">
              {t.setting}
            </span>
          )}
          <code className="text-[11px] text-muted-foreground" dir="ltr">{row.feature.key}</code>
        </div>
        <p className="mt-0.5 text-xs text-muted-foreground">{flagDescription(row.feature)}</p>
      </div>
      <div className="flex items-center gap-2">
        <OnOffBadge on={row.enabled} />
        <SourceBadge source={row.source} />
        {row.hasOverride && <span className="text-xs text-muted-foreground">{t.presetValue(row.presetValue)}</span>}
      </div>
      <div className="flex w-56 items-center justify-end gap-3">
        {row.hasOverride && (
          <button type="button" onClick={onReset} className="inline-flex items-center gap-1 text-xs text-primary hover:underline">
            <RotateCcw className="h-3.5 w-3.5" /> {t.resetToPreset}
          </button>
        )}
        <Toggle checked={row.enabled} onChange={onToggle} label={name} />
      </div>
    </li>
  );
}

'use client';

import { useMemo, useState } from 'react';
import { AlertTriangle } from 'lucide-react';
import type { FeatureCatalogItem, FeaturePreset } from '@gym360/ui';
import { useFlagCatalog, useFlagPresets, useUpdatePreset } from '@/lib/hooks';
import { useFlagsText } from '@/lib/feature-flags-text';
import { Toggle, useConfirmedAction } from './flags-shell';

/** Edit which flags pt_module and business turn on, with an "N gyms affected" warning. */
export function PresetEditor() {
  const { t } = useFlagsText();
  const { data: presetsData, isError } = useFlagPresets();
  const { data: catalogData } = useFlagCatalog();
  const presets = presetsData?.data ?? [];
  const catalog = catalogData?.data ?? [];

  if (isError) return <p className="text-sm text-red-600">{t.loadError}</p>;

  return (
    <div className="grid gap-5 xl:grid-cols-2">
      {presets.map((preset) => (
        <PresetCard key={preset.code} preset={preset} catalog={catalog} />
      ))}
    </div>
  );
}

function PresetCard({ preset, catalog }: { preset: FeaturePreset; catalog: FeatureCatalogItem[] }) {
  const { t, flagName, presetName } = useFlagsText();
  const [draft, setDraft] = useState<Record<string, boolean>>({});
  const update = useUpdatePreset();
  const { ask, dialog } = useConfirmedAction();

  const changes = useMemo(
    () => Object.entries(draft).filter(([key, on]) => (preset.values[key] ?? false) !== on),
    [draft, preset.values],
  );
  const valueOf = (key: string) => draft[key] ?? preset.values[key] ?? false;

  const save = () =>
    ask({
      title: t.confirmPresetEditTitle,
      description: t.confirmPresetEditBody(changes.length, preset.gymCount),
      run: async () => {
        await update.mutateAsync({ code: preset.code, values: Object.fromEntries(changes) });
        setDraft({});
      },
    });

  return (
    <section className="overflow-hidden rounded-xl border border-border bg-card">
      <header className="flex items-center gap-3 border-b border-border px-4 py-3">
        <div>
          <h3 className="font-semibold">{presetName(preset, preset.code)}</h3>
          <code className="text-xs text-muted-foreground" dir="ltr">{preset.code}</code>
        </div>
        <button
          type="button"
          onClick={save}
          disabled={changes.length === 0}
          className="ms-auto rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
        >
          {t.save}
        </button>
      </header>
      <div className="flex items-center gap-2 bg-amber-50 px-4 py-2 text-sm text-amber-800 dark:bg-amber-950/40 dark:text-amber-200">
        <AlertTriangle className="h-4 w-4 shrink-0" />
        <span>{t.affectsGyms(preset.gymCount)}</span>
        {changes.length > 0 && <span className="ms-auto font-medium">{t.unsaved(changes.length)}</span>}
      </div>
      <ul className="max-h-[32rem] divide-y divide-border overflow-y-auto">
        {catalog.map((feature) => (
          <li key={feature.key} className="flex items-center gap-3 px-4 py-2.5">
            <div className="flex-1">
              <span className="text-sm font-medium">{flagName(feature)}</span>
              <span className="ms-2 text-[11px] uppercase text-muted-foreground">{feature.module}</span>
            </div>
            <Toggle
              checked={valueOf(feature.key)}
              onChange={(on) => setDraft((d) => ({ ...d, [feature.key]: on }))}
              label={flagName(feature)}
            />
          </li>
        ))}
      </ul>
      {dialog}
    </section>
  );
}

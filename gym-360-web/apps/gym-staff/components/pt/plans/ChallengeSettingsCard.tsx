'use client';

import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { usePlanSettings, useSavePlanSettings } from '@/lib/hooks/usePlanSettings';
import { canSaveChallenge, draftOf, type ChallengeDraft, type ChallengeSettings } from '@/lib/challenge';
import { useChallengeText, type ChallengeText } from '@/lib/challenge-text';

const numberOrBlank = (value: string): number | '' => (value === '' ? '' : Number(value));

/** mem.first_month_challenge — target, window and reward, for the owner and managers. Hidden while the flag is off. */
export function ChallengeSettingsCard() {
  const t = useChallengeText();
  const { data } = usePlanSettings();
  const challenge = data?.challenge;
  return challenge ? <ChallengeForm settings={challenge} t={t} /> : null;
}

function ChallengeForm({ settings, t }: { settings: ChallengeSettings; t: ChallengeText }) {
  const save = useSavePlanSettings();
  const [draft, setDraft] = useState<ChallengeDraft>(() => draftOf(settings));
  useEffect(() => setDraft(draftOf(settings)), [settings]);
  const set = (patch: Partial<ChallengeDraft>) => setDraft((d) => ({ ...d, ...patch }));

  const onSave = () => {
    if (!canSaveChallenge(draft, settings)) return;
    const challenge = { target: Number(draft.target), windowDays: Number(draft.windowDays), rewardTextAr: draft.rewardTextAr, rewardTextEn: draft.rewardTextEn };
    save.mutate({ challenge }, { onSuccess: () => toast.success(t.saved), onError: () => toast.error(t.saveError) });
  };

  return (
    <section className="rounded-lg border p-4 space-y-3" aria-labelledby="challenge-settings-title">
      <h2 id="challenge-settings-title" className="text-base font-semibold">{t.settingsTitle}</h2>
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <label htmlFor="challenge-target">{t.targetLabel}</label>
        <input id="challenge-target" type="number" min={settings.minTarget} max={settings.maxTarget} value={draft.target}
          onChange={(e) => set({ target: numberOrBlank(e.target.value) })} className="w-20 rounded-md border px-2 py-1" />
        <span>{t.targetUnit}</span>
        <label htmlFor="challenge-window">{t.windowLabel}</label>
        <input id="challenge-window" type="number" min={settings.minWindowDays} max={settings.maxWindowDays} value={draft.windowDays}
          onChange={(e) => set({ windowDays: numberOrBlank(e.target.value) })} className="w-20 rounded-md border px-2 py-1" />
        <span>{t.windowUnit}</span>
      </div>
      <RewardField id="reward-ar" label={t.rewardAr} dir="rtl" value={draft.rewardTextAr} placeholder={settings.defaultRewardTextAr}
        max={settings.maxRewardLength} onChange={(v) => set({ rewardTextAr: v })} />
      <RewardField id="reward-en" label={t.rewardEn} dir="ltr" value={draft.rewardTextEn} placeholder={settings.defaultRewardTextEn}
        max={settings.maxRewardLength} onChange={(v) => set({ rewardTextEn: v })} />
      <p className="text-xs text-muted-foreground">{t.rewardHint} {t.settingsHint}</p>
      <button type="button" disabled={save.isPending || !canSaveChallenge(draft, settings)} onClick={onSave}
        className="rounded-md bg-primary px-3 py-1 text-sm font-medium text-primary-foreground disabled:opacity-50">
        {t.save}
      </button>
    </section>
  );
}

interface RewardFieldProps {
  id: string;
  label: string;
  dir: 'rtl' | 'ltr';
  value: string;
  placeholder: string;
  max: number;
  onChange: (value: string) => void;
}

function RewardField({ id, label, dir, value, placeholder, max, onChange }: RewardFieldProps) {
  return (
    <div className="space-y-1 text-sm">
      <label htmlFor={id} className="block">{label}</label>
      <input id={id} dir={dir} value={value} placeholder={placeholder} maxLength={max}
        onChange={(e) => onChange(e.target.value)} className="w-full rounded-md border px-2 py-1" />
    </div>
  );
}

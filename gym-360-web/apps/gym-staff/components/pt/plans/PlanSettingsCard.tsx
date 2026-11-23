'use client';

import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { useDocumentLocale } from '@gym360/ui';
import { usePlanSettings, useSavePlanSettings } from '@/lib/hooks/usePlanSettings';
import { canSaveInactiveDays, isInactiveDaysValid, type DaysInput } from '@/lib/plan-settings';

const TEXT = {
  en: {
    title: 'Follow-up',
    inactiveLabel: 'A member needs a nudge after',
    days: 'days without a workout',
    hint: 'Coaches see these members first in "My clients\' plans", with a reminder and a WhatsApp button.',
    save: 'Save',
    saved: 'Saved',
    error: 'Could not save. Try again.',
  },
  ar: {
    title: 'المتابعة',
    inactiveLabel: 'العضو يحتاج متابعة بعد',
    days: 'أيام من غير تمرين',
    hint: 'المدربين بيشوفوا الأعضاء دول الأول في "خطط عملائي"، ومعاهم زرار تذكير وواتساب.',
    save: 'حفظ',
    saved: 'تم الحفظ',
    error: 'تعذّر الحفظ، حاول مرة أخرى.',
  },
};

/** pt.training_plan — gym-wide plan settings for the owner and managers. */
export function PlanSettingsCard() {
  const t = TEXT[useDocumentLocale() === 'ar' ? 'ar' : 'en'];
  const { data } = usePlanSettings();
  const save = useSavePlanSettings();
  const [days, setDays] = useState<DaysInput>('');

  useEffect(() => {
    if (data) setDays(data.inactiveDays);
  }, [data]);

  if (!data) return null;
  const onSave = () => {
    if (!isInactiveDaysValid(days, data)) return;
    save.mutate({ inactiveDays: days }, { onSuccess: () => toast.success(t.saved), onError: () => toast.error(t.error) });
  };

  return (
    <section className="rounded-lg border p-4 space-y-2" aria-labelledby="plan-settings-title">
      <h2 id="plan-settings-title" className="text-base font-semibold">{t.title}</h2>
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <label htmlFor="inactive-days">{t.inactiveLabel}</label>
        <input
          id="inactive-days"
          type="number"
          min={data.minInactiveDays}
          max={data.maxInactiveDays}
          value={days}
          onChange={(e) => setDays(e.target.value === '' ? '' : Number(e.target.value))}
          className="w-20 rounded-md border px-2 py-1"
        />
        <span>{t.days}</span>
        <button
          type="button"
          disabled={save.isPending || !canSaveInactiveDays(days, data)}
          onClick={onSave}
          className="rounded-md bg-primary px-3 py-1 text-sm font-medium text-primary-foreground disabled:opacity-50"
        >
          {t.save}
        </button>
      </div>
      <p className="text-xs text-muted-foreground">{t.hint}</p>
    </section>
  );
}

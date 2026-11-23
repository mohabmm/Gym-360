'use client';

import Link from 'next/link';
import { toast } from 'sonner';
import { PageHeader, useDocumentLocale } from '@gym360/ui';
import { usePlanText, type PlanText } from '@/lib/plan-text';
import {
  useArchivePlanTemplate, useDuplicatePlanTemplate, usePlanTemplates, type PlanTemplateSummary,
} from '@/lib/hooks/usePlanTemplates';
import { PtCell, PtState, PtTable } from '@/components/pt/PtTable';
import { ChallengeSettingsCard } from '@/components/pt/plans/ChallengeSettingsCard';
import { PlanSettingsCard } from '@/components/pt/plans/PlanSettingsCard';

/** pt.training_plan — the gym's plan templates (owner and managers). Coaches use them in the trainer app. */
export default function PlanTemplatesPage() {
  const t = usePlanText();
  const { data = [], isLoading, isError } = usePlanTemplates();
  const gymTemplates = data.filter((x) => x.scope === 'Gym');
  const isEmpty = !isLoading && !isError && gymTemplates.length === 0;

  return (
    <div className="space-y-6">
      <PageHeader
        title={t.title}
        description={t.description}
        actions={<Link href="/pt/plan-templates/new" className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground">{t.newTemplate}</Link>}
      />
      <PlanSettingsCard />
      <ChallengeSettingsCard />
      {isLoading && <PtState text="…" />}
      {isError && <PtState text={t.loadError} />}
      {isEmpty && <PtState text={t.empty} />}
      {gymTemplates.length > 0 && (
        <PtTable head={[t.name, t.goal, t.duration, '']}>
          {gymTemplates.map((x) => <TemplateRow key={x.id} template={x} t={t} />)}
        </PtTable>
      )}
    </div>
  );
}

function TemplateRow({ template: x, t }: { template: PlanTemplateSummary; t: PlanText }) {
  const isArabic = useDocumentLocale() === 'ar';
  const archive = useArchivePlanTemplate();
  const duplicate = useDuplicatePlanTemplate();
  const onArchive = () => {
    if (!window.confirm(t.archiveConfirm)) return;
    archive.mutate(x.id, { onSuccess: () => toast.success(t.archived), onError: () => toast.error(t.loadError) });
  };
  return (
    <tr>
      <PtCell className="font-medium">
        {isArabic && x.nameAr ? x.nameAr : x.name}
        {x.isSample && <span className="ms-2 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">{t.sample}</span>}
      </PtCell>
      <PtCell>{x.goal === 'Other' && x.goalText ? x.goalText : t.goals[x.goal]}</PtCell>
      <PtCell>{t.daysPerWeek(x.daysPerWeek)} · {t.weeks(x.durationWeeks)}</PtCell>
      <PtCell className="text-end whitespace-nowrap space-x-3 rtl:space-x-reverse">
        <Link href={`/pt/plan-templates/${x.id}`} className="text-sm font-medium text-primary hover:underline">{t.edit}</Link>
        <button type="button" className="text-sm text-primary hover:underline"
          onClick={() => duplicate.mutate({ id: x.id, name: t.copyName(x.name) })}>{t.duplicate}</button>
        <button type="button" className="text-sm text-muted-foreground hover:text-foreground" onClick={onArchive}>{t.archive}</button>
      </PtCell>
    </tr>
  );
}

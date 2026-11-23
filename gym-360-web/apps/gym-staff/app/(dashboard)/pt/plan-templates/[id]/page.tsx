'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { PageHeader } from '@gym360/ui';
import { blankDraft, draftFromDocument } from '@/lib/plan-draft';
import { usePlanText } from '@/lib/plan-text';
import { usePlanTemplate } from '@/lib/hooks/usePlanTemplates';
import { PtState } from '@/components/pt/PtTable';
import { PlanTemplateEditor } from '@/components/pt/plans/PlanTemplateEditor';

/** Create (`/pt/plan-templates/new`) or edit a gym plan template. */
export default function PlanTemplateEditPage() {
  const t = usePlanText();
  const { id } = useParams<{ id: string }>();
  const templateId = id === 'new' ? null : id;
  const { data, isLoading, isError } = usePlanTemplate(templateId);

  const header = (
    <PageHeader
      title={templateId ? t.edit : t.newTemplate}
      actions={<Link href="/pt/plan-templates" className="text-sm text-primary hover:underline">{t.back}</Link>}
    />
  );
  if (!templateId) return <div className="space-y-6">{header}<PlanTemplateEditor templateId={null} initial={blankDraft()} /></div>;
  if (isLoading) return <PtState text="…" />;
  if (isError || !data) return <PtState text={t.loadError} />;
  const s = data.summary;
  const initial = draftFromDocument(data.document, { name: s.name, nameAr: s.nameAr ?? '', goal: s.goal, goalText: s.goalText ?? '' });
  return <div className="space-y-6">{header}<PlanTemplateEditor key={s.id} templateId={s.id} initial={initial} /></div>;
}

'use client';

import Link from 'next/link';
import { toast } from 'sonner';
import { PageHeader, formatDateTime } from '@gym360/ui';
import { type PackageInterestLead, useDismissInterest, usePendingInterests, whatsAppUrl } from '@/lib/hooks/usePt';
import { type PtText, usePtText } from '@/lib/pt-text';
import { PtCell, PtState, PtTable } from '@/components/pt/PtTable';

function requestLabel(t: PtText, lead: PackageInterestLead): string {
  const what = lead.source === 'Renewal' ? t.sourceRenewal : t.sourceAfterTrial;
  return lead.recommendedSessions ? `${what} · ${t.sessionsCount(lead.recommendedSessions)}` : what;
}

/** pt.package_interest — trainees who tapped "I'm interested"; the desk calls them and sells the package. */
export default function PackageInterestsPage() {
  const t = usePtText();
  const { data: leads = [], isLoading, isError } = usePendingInterests();
  const dismiss = useDismissInterest();

  const close = (id: string) =>
    dismiss.mutate(id, {
      onSuccess: () => toast.success(t.leadClosed),
      onError: () => toast.error(t.loadError),
    });

  return (
    <div className="space-y-6">
      <PageHeader title={t.interestsTitle} description={t.interestsDescription} />
      {isLoading && <PtState text="…" />}
      {isError && <PtState text={t.loadError} />}
      {!isLoading && !isError && leads.length === 0 && <PtState text={t.interestsEmpty} />}
      {leads.length > 0 && (
        <PtTable head={[t.member, t.coach, t.request, t.requestedOn, '']}>
          {leads.map((lead) => (
            <tr key={lead.id}>
              <PtCell className="font-medium">
                <Link href={`/members/${lead.memberId}`} className="hover:underline">{lead.memberName}</Link>
                {lead.memberPhone && <span className="block text-xs text-muted-foreground" dir="ltr">{lead.memberPhone}</span>}
              </PtCell>
              <PtCell>{lead.trainerName}</PtCell>
              <PtCell>{requestLabel(t, lead)}</PtCell>
              <PtCell>{formatDateTime(lead.createdAt)}</PtCell>
              <PtCell className="text-end whitespace-nowrap space-x-3 rtl:space-x-reverse">
                {lead.memberPhone && (
                  <>
                    <a href={`tel:${lead.memberPhone}`} className="text-sm font-medium text-primary hover:underline">{t.call}</a>
                    <a href={whatsAppUrl(lead.memberPhone)} target="_blank" rel="noreferrer" className="text-sm font-medium text-primary hover:underline">{t.whatsapp}</a>
                  </>
                )}
                <Link href={`/members/${lead.memberId}`} className="text-sm font-medium text-primary hover:underline">{t.openProfile}</Link>
                <button
                  type="button"
                  onClick={() => close(lead.id)}
                  disabled={dismiss.isPending}
                  className="text-sm text-muted-foreground hover:text-foreground disabled:opacity-50"
                >
                  {t.closeLead}
                </button>
              </PtCell>
            </tr>
          ))}
        </PtTable>
      )}
    </div>
  );
}

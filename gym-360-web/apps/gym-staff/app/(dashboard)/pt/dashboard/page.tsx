'use client';

import { useState } from 'react';
import { KpiCard, PageHeader } from '@gym360/ui';
import { Gift, Repeat, CheckCircle2, Percent } from 'lucide-react';
import { emptyRow, usePtDashboard, type PtCoachRow } from '@/lib/hooks/usePt';
import { usePtText, type PtText } from '@/lib/pt-text';
import { PtCell, PtState, PtTable } from '@/components/pt/PtTable';

const PERIODS = [7, 30, 90] as const;

const percent = (rate: number) => `${Math.round(rate ?? 0)}%`;  // API sends 0–100

function PeriodPicker({ days, onChange, t }: { days: number; onChange: (d: number) => void; t: PtText }) {
  return (
    <div className="inline-flex rounded-lg border bg-card p-1">
      {PERIODS.map((d) => (
        <button
          key={d}
          onClick={() => onChange(d)}
          className={`rounded-md px-3 py-1.5 text-sm ${d === days ? 'bg-primary text-primary-foreground' : 'hover:bg-muted'}`}
        >
          {t.period(d)}
        </button>
      ))}
    </div>
  );
}

function Totals({ totals, t }: { totals: PtCoachRow; t: PtText }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      <KpiCard value={percent(totals.conversionRate)} label={t.conversion} icon={<Percent className="h-5 w-5" />} variant="success" />
      <KpiCard value={`${totals.trialsConverted} / ${totals.trialsFinished}`} label={`${t.trialsConverted} / ${t.trialsFinished}`} icon={<Gift className="h-5 w-5" />} variant="info" />
      <KpiCard value={String(totals.renewals)} label={t.renewals} icon={<Repeat className="h-5 w-5" />} variant="default" />
      <KpiCard value={String(totals.sessionsLogged)} label={t.sessionsLogged} icon={<CheckCircle2 className="h-5 w-5" />} variant="default" />
    </div>
  );
}

/** pt.owner_dashboard — trial-to-paid conversion per coach, renewals, sessions logged. */
export default function PtDashboardPage() {
  const t = usePtText();
  const [days, setDays] = useState<number>(30);
  const { data, isLoading, isError } = usePtDashboard(days);
  const totals = data?.totals ?? emptyRow();
  const coaches = data?.coaches ?? [];

  return (
    <div className="space-y-6">
      <PageHeader title={t.dashTitle} description={t.dashDescription} actions={<PeriodPicker days={days} onChange={setDays} t={t} />} />
      {isError ? <PtState text={t.loadError} /> : <Totals totals={totals} t={t} />}
      <h2 className="text-lg font-semibold">{t.perCoach}</h2>
      {!isLoading && !isError && coaches.length === 0 && <PtState text={t.noCoaches} />}
      {coaches.length > 0 && (
        <PtTable head={[t.coach, t.trialsFinished, t.trialsConverted, t.conversion, t.newPackages, t.renewals, t.sessionsLogged]}>
          {coaches.map((c) => (
            <tr key={c.trainerId}>
              <PtCell className="font-medium">{c.trainerName}</PtCell>
              <PtCell>{c.trialsFinished}</PtCell>
              <PtCell>{c.trialsConverted}</PtCell>
              <PtCell>{percent(c.conversionRate)}</PtCell>
              <PtCell>{c.newPaidPackages}</PtCell>
              <PtCell>{c.renewals}</PtCell>
              <PtCell>{c.sessionsLogged}</PtCell>
            </tr>
          ))}
        </PtTable>
      )}
      <p className="text-xs text-muted-foreground">{t.conversionNote}</p>
    </div>
  );
}

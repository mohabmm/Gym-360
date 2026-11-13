'use client';

import { useState } from 'react';
import { formatDateTime, type FlagAuditEntry } from '@gym360/ui';
import { useFlagAudit, useFlagCatalog, useFlagGyms } from '@/lib/hooks';
import { useFlagsText } from '@/lib/feature-flags-text';

type FlagsText = ReturnType<typeof useFlagsText>;

const SELECT = 'rounded-md border border-input bg-background px-3 py-2 text-sm';
const dash = (value?: string | null) => value ?? '—';

/** Audit log with filters by gym and flag. */
export function AuditTable() {
  const text = useFlagsText();
  const { t } = text;
  const [gymId, setGymId] = useState('');
  const [featureKey, setFeatureKey] = useState('');
  const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useFlagAudit(gymId, featureKey, page);
  const { data: gymsData } = useFlagGyms('', '');
  const { data: catalogData } = useFlagCatalog();

  const result = data?.data;
  const rows = result?.items ?? [];
  const catalog = catalogData?.data ?? [];
  const nameOfFlag = (key: string) => {
    const f = catalog.find((c) => c.key === key);
    return f ? text.flagName(f) : key;
  };
  const filterBy = (set: (v: string) => void) => (value: string) => { set(value); setPage(1); };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-3">
        <select className={SELECT} value={gymId} onChange={(e) => filterBy(setGymId)(e.target.value)}>
          <option value="">{t.allGyms}</option>
          {(gymsData?.data?.items ?? []).map((g) => <option key={g.gymId} value={g.gymId}>{g.name}</option>)}
        </select>
        <select className={SELECT} value={featureKey} onChange={(e) => filterBy(setFeatureKey)(e.target.value)}>
          <option value="">{t.allFlags}</option>
          {catalog.map((f) => <option key={f.key} value={f.key}>{text.flagName(f)}</option>)}
        </select>
      </div>

      <div className="overflow-x-auto rounded-xl border border-border bg-card">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-muted-foreground">
            <tr>
              {[t.when, t.who, t.gym, t.change, t.flag, t.preset, t.oldValue, t.newValue].map((h) => (
                <th key={h} className="px-4 py-3 text-start font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => <AuditRow key={r.id} entry={r} text={text} nameOfFlag={nameOfFlag} />)}
          </tbody>
        </table>
        {!isLoading && rows.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">{isError ? t.loadError : t.empty}</p>
        )}
      </div>

      <Pager text={text} page={page} hasNext={page * (result?.pageSize ?? 50) < (result?.totalCount ?? 0)} onPage={setPage} />
    </div>
  );
}

function AuditRow({ entry: r, text, nameOfFlag }: { entry: FlagAuditEntry; text: FlagsText; nameOfFlag: (key: string) => string }) {
  return (
    <tr className="border-t border-border">
      <td className="whitespace-nowrap px-4 py-2.5" dir="ltr">{formatDateTime(r.occurredAtUtc)}</td>
      <td className="px-4 py-2.5">{r.actorEmail ?? r.actorUserId}</td>
      <td className="px-4 py-2.5">{dash(r.gymName ?? r.gymId)}</td>
      <td className="px-4 py-2.5">{text.changeType(r.changeType)}</td>
      <td className="px-4 py-2.5">{r.featureKey ? nameOfFlag(r.featureKey) : '—'}</td>
      <td className="px-4 py-2.5">{dash(r.presetCode)}</td>
      <td className="px-4 py-2.5">{dash(r.oldValue)}</td>
      <td className="px-4 py-2.5 font-medium">{dash(r.newValue)}</td>
    </tr>
  );
}

function Pager({ text, page, hasNext, onPage }: { text: FlagsText; page: number; hasNext: boolean; onPage: (page: number) => void }) {
  return (
    <div className="flex items-center justify-end gap-2 text-sm">
      <button type="button" disabled={page <= 1} onClick={() => onPage(page - 1)}
        className="rounded-md border border-border px-3 py-1.5 disabled:opacity-50">{text.t.previous}</button>
      <button type="button" disabled={!hasNext} onClick={() => onPage(page + 1)}
        className="rounded-md border border-border px-3 py-1.5 disabled:opacity-50">{text.t.next}</button>
    </div>
  );
}

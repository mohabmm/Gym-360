'use client';

import { useState } from 'react';
import { formatDateTime } from '@gym360/ui';
import { useFlagAudit, useFlagCatalog, useFlagGyms } from '@/lib/hooks';
import { useFlagsText } from '@/lib/feature-flags-text';

/** Audit log with filters by gym and flag. */
export function AuditTable() {
  const { t, flagName, changeType } = useFlagsText();
  const [gymId, setGymId] = useState('');
  const [featureKey, setFeatureKey] = useState('');
  const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useFlagAudit(gymId, featureKey, page);
  const { data: gymsData } = useFlagGyms('', '');
  const { data: catalogData } = useFlagCatalog();

  const rows = data?.data?.items ?? [];
  const total = data?.data?.totalCount ?? 0;
  const pageSize = data?.data?.pageSize ?? 50;
  const catalog = catalogData?.data ?? [];
  const nameOfFlag = (key?: string | null) => {
    const f = catalog.find((c) => c.key === key);
    return f ? flagName(f) : key ?? '—';
  };
  const select = 'rounded-md border border-input bg-background px-3 py-2 text-sm';

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-3">
        <select className={select} value={gymId} onChange={(e) => { setGymId(e.target.value); setPage(1); }}>
          <option value="">{t.allGyms}</option>
          {(gymsData?.data?.items ?? []).map((g) => <option key={g.gymId} value={g.gymId}>{g.name}</option>)}
        </select>
        <select className={select} value={featureKey} onChange={(e) => { setFeatureKey(e.target.value); setPage(1); }}>
          <option value="">{t.allFlags}</option>
          {catalog.map((f) => <option key={f.key} value={f.key}>{flagName(f)}</option>)}
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
            {rows.map((r) => (
              <tr key={r.id} className="border-t border-border">
                <td className="whitespace-nowrap px-4 py-2.5" dir="ltr">{formatDateTime(r.occurredAtUtc)}</td>
                <td className="px-4 py-2.5">{r.actorEmail ?? r.actorUserId}</td>
                <td className="px-4 py-2.5">{r.gymName ?? (r.gymId ? r.gymId : '—')}</td>
                <td className="px-4 py-2.5">{changeType(r.changeType)}</td>
                <td className="px-4 py-2.5">{r.featureKey ? nameOfFlag(r.featureKey) : '—'}</td>
                <td className="px-4 py-2.5">{r.presetCode ?? '—'}</td>
                <td className="px-4 py-2.5">{r.oldValue ?? '—'}</td>
                <td className="px-4 py-2.5 font-medium">{r.newValue ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {!isLoading && rows.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">{isError ? t.loadError : t.empty}</p>
        )}
      </div>

      <div className="flex items-center justify-end gap-2 text-sm">
        <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}
          className="rounded-md border border-border px-3 py-1.5 disabled:opacity-50">{t.previous}</button>
        <button type="button" disabled={page * pageSize >= total} onClick={() => setPage((p) => p + 1)}
          className="rounded-md border border-border px-3 py-1.5 disabled:opacity-50">{t.next}</button>
      </div>
    </div>
  );
}

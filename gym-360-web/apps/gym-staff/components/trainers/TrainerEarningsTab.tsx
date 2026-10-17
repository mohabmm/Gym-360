import { Edit } from 'lucide-react';
import type { TrainerEarnings, TrainerRate } from '@/lib/hooks/useTrainerEarnings';

interface RateCardProps {
  rate: TrainerRate | null | undefined;
  loading: boolean;
  onEditRate: () => void;
}

function RateValue({ rate, loading, onEditRate }: RateCardProps) {
  const currentRate = rate?.ratePerSession ?? null;
  if (loading) return <p className="text-sm text-muted-foreground">Loading rate…</p>;
  if (currentRate !== null) {
    return (
      <div className="flex items-baseline gap-2">
        <span className="text-3xl font-heading font-bold text-primary">{currentRate}</span>
        <span className="text-lg font-medium text-muted-foreground">EGP / session</span>
      </div>
    );
  }
  return (
    <div className="flex items-center gap-3 text-sm text-muted-foreground">
      <span>No rate configured.</span>
      <button
        onClick={onEditRate}
        className="text-primary hover:underline font-medium"
      >
        Set rate now
      </button>
    </div>
  );
}

function RateCard(props: RateCardProps) {
  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-heading font-semibold">Session Rate</h3>
        <button
          onClick={props.onEditRate}
          className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium hover:bg-muted transition-colors"
        >
          <Edit className="h-4 w-4" /> Edit Rate
        </button>
      </div>
      <RateValue {...props} />
      {props.rate?.effectiveFrom && (
        <p className="text-xs text-muted-foreground mt-2">
          Effective from {new Date(props.rate.effectiveFrom).toLocaleDateString()}
        </p>
      )}
    </div>
  );
}

function SessionBreakdown({ records }: { records: TrainerEarnings['records'] }) {
  if (records.length === 0) {
    return <p className="text-sm text-muted-foreground">No sessions completed this month.</p>;
  }
  return (
    <div>
      <h4 className="text-sm font-medium text-muted-foreground mb-3">Session Breakdown</h4>
      <div className="divide-y divide-border rounded-lg border border-border overflow-hidden">
        {records.map((record) => (
          <div
            key={record.sessionId}
            className="flex items-center justify-between px-4 py-3 text-sm hover:bg-muted/20 transition-colors"
          >
            <div>
              <p className="font-medium font-mono text-xs text-muted-foreground">
                Session {record.sessionId.slice(0, 8)}…
              </p>
              <p className="text-xs text-muted-foreground">
                {new Date(record.earnedAt).toLocaleDateString()}
              </p>
            </div>
            <span className="font-semibold text-primary">{record.rateApplied} EGP</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function EarningsSummary({ earnings, loading }: { earnings: TrainerEarnings | undefined; loading: boolean }) {
  if (loading) return <p className="text-sm text-muted-foreground">Loading earnings…</p>;
  if (!earnings) return <p className="text-sm text-muted-foreground">Failed to load earnings data.</p>;
  return (
    <>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
        <div className="rounded-lg bg-muted/40 p-4">
          <p className="text-xs text-muted-foreground font-medium">Total Sessions</p>
          <p className="text-2xl font-heading font-bold mt-1">{earnings.totalSessions}</p>
        </div>
        <div className="rounded-lg bg-muted/40 p-4">
          <p className="text-xs text-muted-foreground font-medium">Total Earned</p>
          <p className="text-2xl font-heading font-bold mt-1 text-primary">
            {earnings.totalEarningsEgp.toLocaleString()} EGP
          </p>
        </div>
      </div>
      <SessionBreakdown records={earnings.records} />
    </>
  );
}

interface Props extends RateCardProps {
  earnings: TrainerEarnings | undefined;
  earningsLoading: boolean;
}

export function TrainerEarningsTab({ earnings, earningsLoading, ...rate }: Props) {
  return (
    <div className="space-y-4">
      {/* Rate configuration card */}
      <RateCard {...rate} />

      {/* Monthly earnings summary */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold mb-4">Earnings This Month</h3>
        <EarningsSummary earnings={earnings} loading={earningsLoading} />
      </div>
    </div>
  );
}

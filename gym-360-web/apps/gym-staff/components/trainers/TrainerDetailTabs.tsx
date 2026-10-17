import Link from 'next/link';
import { StatusBadge } from '@gym360/ui';
import type { TrainerListItem } from '@gym360/ui';
import { Star } from 'lucide-react';
import type { TrainerClientItem, TrainerSessionItem } from '@/lib/hooks/useTrainers';

export const TRAINER_TABS = ['Profile', 'Members', 'Sessions', 'Ratings', 'Earnings'] as const;
export type TrainerTab = (typeof TRAINER_TABS)[number];

export function TrainerTabBar({ active, onChange }: { active: TrainerTab; onChange: (tab: TrainerTab) => void }) {
  return (
    <div className="border-b border-border">
      <div className="flex gap-1">
        {TRAINER_TABS.map((tab) => (
          <button
            key={tab}
            onClick={() => onChange(tab)}
            className={`px-5 py-3 text-sm font-medium border-b-2 -mb-px transition-colors ${
              active === tab
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>
    </div>
  );
}

const NotProvided = () => <span className="text-muted-foreground italic">Not provided</span>;

function InfoItem({ label, className = 'mt-1 font-medium', wide, children }: { label: string; className?: string; wide?: boolean; children: React.ReactNode }) {
  return (
    <div className={wide ? 'sm:col-span-2' : undefined}>
      <dt className="text-muted-foreground font-medium">{label}</dt>
      <dd className={className}>{children}</dd>
    </div>
  );
}

function SpecializationChips({ specs }: { specs: string[] }) {
  if (specs.length === 0) return <span className="text-muted-foreground italic text-xs">None listed.</span>;
  return (
    <>
      {specs.map((s) => (
        <span key={s} className="px-2.5 py-0.5 rounded-full bg-accent text-accent-foreground text-xs font-medium border">
          {s}
        </span>
      ))}
    </>
  );
}

export function TrainerProfileTab({ trainer }: { trainer: TrainerListItem }) {
  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      <h3 className="font-heading font-semibold mb-4">Trainer Information</h3>
      <dl className="grid grid-cols-1 sm:grid-cols-2 gap-y-4 gap-x-6 text-sm">
        <InfoItem label="Status" className="mt-1">
          <StatusBadge variant={trainer.status} />
        </InfoItem>
        <InfoItem label="Email" className="mt-1 font-medium break-all">
          {trainer.email ? (
            <a href={`mailto:${trainer.email}`} className="hover:text-primary hover:underline">
              {trainer.email}
            </a>
          ) : (
            <NotProvided />
          )}
        </InfoItem>
        <InfoItem label="Phone">
          {trainer.phone ? (
            <a href={`tel:${trainer.phone}`} className="hover:text-primary hover:underline">
              {trainer.phone}
            </a>
          ) : (
            <NotProvided />
          )}
        </InfoItem>
        <InfoItem label="Active Clients">{trainer.activeMemberCount}</InfoItem>
        <InfoItem label="Monthly Score">{trainer.monthlyScore.toFixed(1)}/5</InfoItem>
        <InfoItem label="Rating" className="mt-1 flex items-center gap-1.5 font-medium">
          <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
          {trainer.rating}/5.0
        </InfoItem>
        <InfoItem label="Specializations" className="mt-2 flex flex-wrap gap-2" wide>
          <SpecializationChips specs={trainer.specializations} />
        </InfoItem>
        <InfoItem label="Bio" className="mt-1 whitespace-pre-line leading-relaxed" wide>
          {trainer.bio?.trim() ? (
            trainer.bio
          ) : (
            <span className="text-muted-foreground italic">No bio provided.</span>
          )}
        </InfoItem>
      </dl>
    </div>
  );
}

/** Loading / empty / list switch shared by the Members and Sessions tabs. */
function AsyncList<T>({ loading, items, loadingText, emptyText, children }: {
  loading: boolean;
  items: T[] | undefined;
  loadingText: string;
  emptyText: string;
  children: (items: T[]) => React.ReactNode;
}) {
  if (loading) return <div className="p-10 text-center text-sm text-muted-foreground">{loadingText}</div>;
  if (!items || items.length === 0) return <div className="p-10 text-center text-sm text-muted-foreground">{emptyText}</div>;
  return <div className="divide-y divide-border">{children(items)}</div>;
}

function clientContactLine(c: TrainerClientItem) {
  const separator = c.memberNumber && (c.email || c.phone) ? ' · ' : '';
  return `${c.memberNumber ? `#${c.memberNumber}` : ''}${separator}${c.email ?? c.phone ?? ''}`;
}

function ClientRow({ client: c }: { client: TrainerClientItem }) {
  return (
    <Link
      href={`/members/${c.id}`}
      className="flex items-center justify-between px-6 py-4 hover:bg-muted/30 transition-colors"
    >
      <div>
        <p className="font-medium text-sm">{c.name}</p>
        <p className="text-xs text-muted-foreground mt-0.5">{clientContactLine(c)}</p>
      </div>
      <span className="text-xs text-muted-foreground">
        Joined {new Date(c.joinedAt).toLocaleDateString()}
      </span>
    </Link>
  );
}

export function TrainerMembersTab({ trainer, clients, loading }: { trainer: TrainerListItem; clients: TrainerClientItem[] | undefined; loading: boolean }) {
  return (
    <div className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
      <div className="flex items-center justify-between p-6 border-b border-border">
        <div>
          <h3 className="font-heading font-semibold">Assigned Clients</h3>
          <p className="text-xs text-muted-foreground mt-1">
            {trainer.activeMemberCount} active client{trainer.activeMemberCount === 1 ? '' : 's'} training with {trainer.name}
          </p>
        </div>
      </div>
      <AsyncList items={clients} loading={loading} loadingText="Loading clients…" emptyText="No clients assigned yet.">
        {(items) => items.map((c) => <ClientRow key={c.id} client={c} />)}
      </AsyncList>
    </div>
  );
}

function SessionRow({ session: s }: { session: TrainerSessionItem }) {
  return (
    <div className="flex items-center justify-between px-6 py-3 text-sm">
      <div>
        <p className="font-medium">{new Date(s.scheduledStart).toLocaleString()}</p>
        <p className="text-xs text-muted-foreground mt-0.5 font-mono">
          Member {s.memberId.slice(0, 8)}…{s.type ? ` · ${s.type}` : ''}
        </p>
      </div>
      <span className="text-xs px-2 py-0.5 rounded-full bg-muted">{s.status}</span>
    </div>
  );
}

export function TrainerSessionsTab({ sessions, loading }: { sessions: TrainerSessionItem[] | undefined; loading: boolean }) {
  return (
    <div className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
      <div className="p-6 border-b border-border">
        <h3 className="font-heading font-semibold">Session History</h3>
        <p className="text-xs text-muted-foreground mt-1">Sessions completed and upcoming for this trainer</p>
      </div>
      <AsyncList items={sessions} loading={loading} loadingText="Loading sessions…" emptyText="No sessions recorded yet.">
        {(items) => items.map((s) => <SessionRow key={s.id} session={s} />)}
      </AsyncList>
    </div>
  );
}

export function TrainerRatingsTab() {
  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between mb-5 flex-wrap gap-2">
        <h3 className="font-heading font-semibold">Client Ratings</h3>
        <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 px-2.5 py-1 rounded-lg">
          Manager eyes only — trainers see aggregated scores only
        </p>
      </div>
      <div className="p-10 text-center text-sm text-muted-foreground">
        No ratings submitted yet.
      </div>
    </div>
  );
}

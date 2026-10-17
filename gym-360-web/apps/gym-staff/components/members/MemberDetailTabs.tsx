import { StatusBadge, formatCurrency, formatDate } from '@gym360/ui';
import type { MemberListItem, PaymentListItem } from '@gym360/ui';
import type { MemberPackage } from '@/lib/hooks/useMembers';
import { UnderlineTabs } from '@/components/UnderlineTabs';

export const MEMBER_TABS = ['Profile', 'Payments', 'Sessions', 'Progress'] as const;
export type MemberTab = (typeof MEMBER_TABS)[number];

const spaced = (v: string | undefined) => (v ?? '').replace(/_/g, ' ');

function Field({ label, children, className = 'font-medium' }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <div><dt className="text-muted-foreground">{label}</dt><dd className={className}>{children}</dd></div>
  );
}

function ProfileTab({ member }: { member: MemberListItem }) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4">Personal Information</h3>
        <dl className="grid grid-cols-2 gap-4 text-sm">
          <Field label="Name">{member.name}</Field>
          <Field label="Arabic Name">{member.nameAr}</Field>
          <Field label="Email">{member.email}</Field>
          <Field label="Phone">{member.phone}</Field>
          <Field label="Floor">{member.section}</Field>
          <Field label="National ID">{member.nationalId || '—'}</Field>
          <Field label="Status" className=""><StatusBadge variant={member.status} /></Field>
        </dl>
      </div>
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4">Membership</h3>
        <dl className="grid grid-cols-2 gap-4 text-sm">
          <Field label="Type" className="font-medium capitalize">{spaced(member.membershipType)}</Field>
          <Field label="Joined">{formatDate(member.joinDate)}</Field>
          <Field label="Expires">{formatDate(member.expiryDate)}</Field>
          <Field label="Last Check-in">{member.lastCheckIn ? formatDate(member.lastCheckIn) : 'Never'}</Field>
        </dl>
      </div>
    </div>
  );
}

function PaymentsTab({ payments }: { payments: PaymentListItem[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Payment History</h3>
      <div className="space-y-3">
        {payments.slice(0, 3).map((p) => (
          <div key={p.id} className="flex items-center justify-between py-2 border-b last:border-0">
            <div>
              <p className="text-sm font-medium capitalize">{p.type?.replace(/_/g, ' ')}</p>
              <p className="text-xs text-muted-foreground">{formatDate(p.createdAt)} &middot; {p.method?.replace(/_/g, ' ')}</p>
            </div>
            <div className="text-right">
              <p className="text-sm font-medium">{formatCurrency(p.amount)}</p>
              <StatusBadge variant={p.status} className="mt-0.5" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function usedPercent(used: number, total: number) {
  return total > 0 ? (used / total) * 100 : 0;
}

function PackageCard({ pkg: p, trainerName }: { pkg: MemberPackage; trainerName: string }) {
  const used = Math.max(0, p.totalSessions - p.remainingSessions);
  return (
    <div className="rounded-lg border p-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium">{p.name}</p>
          <p className="text-xs text-muted-foreground mt-0.5">
            Trainer: <span className="font-medium text-foreground">{trainerName}</span>
          </p>
          <p className="text-xs text-muted-foreground mt-0.5">
            Started {p.createdAt ? formatDate(p.createdAt) : '—'}
            {p.expiresAt ? ` · Expires ${formatDate(p.expiresAt)}` : ''}
          </p>
        </div>
        <div className="text-right">
          <StatusBadge variant={p.status.toLowerCase()} />
          <p className="text-sm font-medium mt-1">{formatCurrency(p.totalPrice)}</p>
        </div>
      </div>
      <div className="mt-3 flex items-center gap-3">
        <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
          <div
            className="h-full bg-emerald-500"
            style={{ width: `${usedPercent(used, p.totalSessions)}%` }}
          />
        </div>
        <p className="text-xs font-medium tabular-nums whitespace-nowrap">
          {used}/{p.totalSessions} used · {p.remainingSessions} left
        </p>
      </div>
    </div>
  );
}

interface SessionsTabProps {
  packages: MemberPackage[];
  loading: boolean;
  trainerNameById: Map<string, string>;
}

function PackageList({ packages, loading, trainerNameById }: SessionsTabProps) {
  if (loading) return <p className="text-sm text-muted-foreground">Loading packages…</p>;
  if (packages.length === 0) {
    return <p className="text-sm text-muted-foreground">No trainer assigned yet. Use "Assign Trainer" to start a package.</p>;
  }
  return (
    <div className="space-y-3">
      {packages.map((p) => (
        <PackageCard key={p.id} pkg={p} trainerName={trainerNameById.get(p.trainerId) ?? '—'} />
      ))}
    </div>
  );
}

function SessionsTab(props: SessionsTabProps) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Training Packages</h3>
      <PackageList {...props} />
    </div>
  );
}

function ProgressTab() {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Progress Tracking</h3>
      <p className="text-sm text-muted-foreground">Body metrics, progress photos, and workout history will load here.</p>
    </div>
  );
}

export function MemberTabBar({ active, onChange }: { active: MemberTab; onChange: (tab: MemberTab) => void }) {
  return <UnderlineTabs tabs={MEMBER_TABS} active={active} onChange={onChange} />;
}

interface MemberTabContentProps extends SessionsTabProps {
  tab: MemberTab;
  member: MemberListItem;
  payments: PaymentListItem[];
}

export function MemberTabContent({ tab, member, payments, ...sessions }: MemberTabContentProps) {
  switch (tab) {
    case 'Profile':
      return <ProfileTab member={member} />;
    case 'Payments':
      return <PaymentsTab payments={payments} />;
    case 'Sessions':
      return <SessionsTab {...sessions} />;
    case 'Progress':
      return <ProgressTab />;
  }
}

import { StatusBadge, KpiCard } from '@gym360/ui';
import type { TrainerListItem } from '@gym360/ui';
import { Star, Users, DollarSign, Mail, Phone } from 'lucide-react';

function initialsOf(name: string | undefined) {
  return (name ?? '').split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2);
}

function rateLabel(loading: boolean, rate: number | null) {
  if (loading) return '…';
  return rate !== null ? `${rate} EGP` : 'Not set';
}

function ContactLine({ trainer }: { trainer: TrainerListItem }) {
  return (
    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2 text-sm">
      <span className="inline-flex items-center gap-1.5 text-muted-foreground">
        <Mail className="h-3.5 w-3.5" />
        {trainer.email ? (
          <a href={`mailto:${trainer.email}`} className="hover:text-primary hover:underline">
            {trainer.email}
          </a>
        ) : (
          <span className="italic">No email on file</span>
        )}
      </span>
      {trainer.phone && (
        <span className="inline-flex items-center gap-1.5 text-muted-foreground">
          <Phone className="h-3.5 w-3.5" />
          <a href={`tel:${trainer.phone}`} className="hover:text-primary hover:underline">
            {trainer.phone}
          </a>
        </span>
      )}
    </div>
  );
}

export function TrainerHeaderCard({ trainer }: { trainer: TrainerListItem }) {
  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      <div className="flex items-start gap-5">
        <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary font-heading font-bold text-xl">
          {initialsOf(trainer.name)}
        </div>
        <div className="flex-1">
          <div className="flex items-center gap-3 flex-wrap">
            <h2 className="text-xl font-heading font-bold">{trainer.name}</h2>
            <StatusBadge variant={trainer.status} />
          </div>
          <p className="text-muted-foreground text-sm mt-1">{trainer.nameAr}</p>
          <ContactLine trainer={trainer} />
          <div className="flex flex-wrap gap-2 mt-3">
            {trainer.specializations.map((spec) => (
              <span
                key={spec}
                className="px-2.5 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium"
              >
                {spec}
              </span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export function TrainerKpis({ trainer, rateLoading, currentRate }: { trainer: TrainerListItem; rateLoading: boolean; currentRate: number | null }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
      <KpiCard
        value={trainer.activeMemberCount.toString()}
        label="Active Clients"
        icon={<Users className="h-5 w-5" />}
        variant="default"
      />
      <KpiCard
        value={trainer.monthlyScore.toString()}
        label="Monthly Score"
        icon={<Star className="h-5 w-5" />}
        variant="info"
      />
      <KpiCard
        value={`${trainer.rating}/5`}
        label="Average Rating"
        icon={<Star className="h-5 w-5" />}
        variant="success"
      />
      <KpiCard
        value={rateLabel(rateLoading, currentRate)}
        label="Session Rate"
        icon={<DollarSign className="h-5 w-5" />}
        variant="default"
      />
    </div>
  );
}

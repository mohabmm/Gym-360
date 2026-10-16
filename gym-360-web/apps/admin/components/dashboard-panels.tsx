import Link from 'next/link';
import { StatusBadge, formatCurrency, type AtRiskGym, type GymListItem } from '@gym360/ui';
import { AlertTriangle, TrendingDown, Pause } from 'lucide-react';

/** "1 gym" / "3 gyms". */
export const gyms = (count: number) => `${count} gym${count === 1 ? '' : 's'}`;

interface PaymentAlertsBarProps {
  overdueCount: number;
  warningCount: number;
  suspendableCount: number;
  onBulkSuspend: () => void;
}

export function PaymentAlertsBar({ overdueCount, warningCount, suspendableCount, onBulkSuspend }: PaymentAlertsBarProps) {
  const total = overdueCount + warningCount;
  if (total === 0) return null;

  return (
    <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 dark:border-amber-800 dark:bg-amber-950/20">
      <AlertTriangle className="h-5 w-5 text-amber-600 shrink-0" />
      <p className="text-sm font-medium text-amber-800 dark:text-amber-300">
        {gyms(total)} with payment issues
        {overdueCount > 0 && (
          <>
            {' '}— <span className="font-semibold">{overdueCount} overdue</span>
          </>
        )}
        {warningCount > 0 && (
          <>
            {overdueCount > 0 ? ',' : ' —'} {warningCount} in warning
          </>
        )}
      </p>
      <div className="ml-auto flex items-center gap-2">
        {suspendableCount > 0 && (
          <button
            onClick={onBulkSuspend}
            className="inline-flex items-center gap-1.5 rounded-lg border border-amber-300 bg-white px-3 py-1.5 text-xs font-medium text-amber-800 hover:bg-amber-100 dark:bg-amber-900/20"
          >
            <Pause className="h-3.5 w-3.5" /> Bulk Suspend Overdue ({suspendableCount})
          </button>
        )}
        <Link
          href="/gyms?paymentStatus=overdue"
          className="text-sm font-medium text-amber-700 hover:text-amber-900 dark:text-amber-400"
        >
          View All
        </Link>
      </div>
    </div>
  );
}

export function RecentRegistrations({ gyms }: { gyms: GymListItem[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Recent Registrations</h3>
      <div className="space-y-3">
        {gyms.map((gym) => (
          <div key={gym.id} className="flex items-center justify-between py-2 border-b last:border-0">
            <div>
              <p className="text-sm font-medium">{gym.name}</p>
              <p className="text-xs text-muted-foreground">{gym.city} &middot; {gym.ownerName}</p>
            </div>
            <StatusBadge variant={gym.status} />
          </div>
        ))}
      </div>
    </div>
  );
}

export function AtRiskGyms({ gyms }: { gyms: AtRiskGym[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-amber-500" />
        At-Risk Gyms
      </h3>
      <div className="space-y-3">
        {gyms.map((gym) => (
          <div key={gym.id} className="flex items-center justify-between py-2 border-b last:border-0">
            <div>
              <p className="text-sm font-medium">{gym.name}</p>
              <p className="text-xs text-muted-foreground">{gym.riskFactors[0]}</p>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-bold text-red-500">{gym.riskScore}%</span>
              <TrendingDown className="h-3.5 w-3.5 text-red-500" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

const TH = 'py-2 px-3 font-semibold text-muted-foreground';

export function TopGymsTable({ gyms }: { gyms: GymListItem[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Top Performing Gyms</h3>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b">
              <th className={`text-left ${TH}`}>Gym</th>
              <th className={`text-left ${TH}`}>Plan</th>
              <th className={`text-right ${TH}`}>Members</th>
              <th className={`text-right ${TH}`}>Revenue</th>
              <th className={`text-left ${TH}`}>Status</th>
            </tr>
          </thead>
          <tbody>
            {gyms.map((gym, index) => (
              <tr key={gym.id} className="border-b last:border-0 hover:bg-muted/50">
                <td className="py-2.5 px-3">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-bold text-muted-foreground w-5">#{index + 1}</span>
                    <div>
                      <p className="font-medium">{gym.name}</p>
                      <p className="text-xs text-muted-foreground">{gym.city}</p>
                    </div>
                  </div>
                </td>
                <td className="py-2.5 px-3"><StatusBadge variant={gym.planName.toLowerCase()} /></td>
                <td className="py-2.5 px-3 text-right font-medium">{gym.activeMemberCount}</td>
                <td className="py-2.5 px-3 text-right font-medium">{formatCurrency(gym.monthlyRevenue)}</td>
                <td className="py-2.5 px-3"><StatusBadge variant={gym.paymentStatus} dot /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

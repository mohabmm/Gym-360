import { ChartContainer } from '@gym360/ui';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';
import type { HeatRow } from '@/lib/analytics';

export function MemberGrowthChart({ data: memberGrowth }: { data: { month: string; active: number; new: number }[] }) {
  return (
    <ChartContainer title="Member Growth" subtitle="Active members and new signups">
      <BarChart data={memberGrowth}>
        <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
        <XAxis dataKey="month" tick={{ fontSize: 12 }} stroke="#9CA3AF" />
        <YAxis tick={{ fontSize: 12 }} stroke="#9CA3AF" />
        <Tooltip />
        <Legend />
        <Bar dataKey="active" fill="#C41E3A" radius={[4, 4, 0, 0]} name="Active" />
        <Bar dataKey="new" fill="#D4AF37" radius={[4, 4, 0, 0]} name="New" />
      </BarChart>
    </ChartContainer>
  );
}

const HEAT_DAYS = ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun'] as const;

function HeatCell({ val }: { val: number }) {
  return (
    <td className="py-1 px-2">
      <div
        className="h-6 w-full rounded flex items-center justify-center text-[10px] font-medium"
        style={{
          backgroundColor: `rgba(196, 30, 58, ${Math.min(val / 50, 1)})`,
          color: val > 25 ? 'white' : '#374151',
        }}
      >
        {val}
      </div>
    </td>
  );
}

export function PeakHoursHeatmap({ rows: peakHours }: { rows: HeatRow[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Peak Hours Heatmap</h3>
      <div className="overflow-x-auto">
        <table className="w-full text-xs">
          <thead>
            <tr>
              <th className="text-left py-1 px-2"></th>
              <th className="text-center py-1 px-2">Mon</th>
              <th className="text-center py-1 px-2">Tue</th>
              <th className="text-center py-1 px-2">Wed</th>
              <th className="text-center py-1 px-2">Thu</th>
              <th className="text-center py-1 px-2">Fri</th>
              <th className="text-center py-1 px-2">Sat</th>
              <th className="text-center py-1 px-2">Sun</th>
            </tr>
          </thead>
          <tbody>
            {peakHours.map((row) => (
              <tr key={row.hour}>
                <td className="py-1 px-2 font-medium">{row.hour}</td>
                {HEAT_DAYS.map((day) => <HeatCell key={day} val={row[day]} />)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export function ClassUtilizationChart({ data: classUtilization }: { data: { name: string; utilization: number }[] }) {
  return (
    <ChartContainer title="Class Utilization" subtitle="Enrollment vs capacity">
      <BarChart data={classUtilization} layout="vertical">
        <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
        <XAxis type="number" tick={{ fontSize: 12 }} stroke="#9CA3AF" domain={[0, 100]} tickFormatter={(v: number) => `${v}%`} />
        <YAxis type="category" dataKey="name" tick={{ fontSize: 11 }} stroke="#9CA3AF" width={120} />
        <Tooltip formatter={(value: number) => `${value}%`} />
        <Bar dataKey="utilization" fill="#C41E3A" radius={[0, 4, 4, 0]} />
      </BarChart>
    </ChartContainer>
  );
}

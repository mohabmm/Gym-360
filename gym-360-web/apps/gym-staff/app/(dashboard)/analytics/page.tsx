'use client';

import { PageHeader, KpiCard, ChartContainer, formatCurrency, formatNumber } from '@gym360/ui';
import { Users, DollarSign, DoorOpen, Dumbbell, BarChart3 } from 'lucide-react';
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';
import { useRevenue } from '@/lib/hooks/useRevenue';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import { useQuery } from '@tanstack/react-query';


export default function AnalyticsPage() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const { data: revenueData } = useRevenue();
  const monthlyRevenue = revenueData?.monthlyRevenue ?? [];
  const totalRevenue = monthlyRevenue.reduce((sum, m) => sum + m.revenue, 0);
  const { data: analytics } = useQuery({
    queryKey: ['gym-analytics', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/analytics`);
      return res.data?.data ?? null;
    },
  });
  const { data: classes } = useQuery({
    queryKey: ['classes-utilization', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/classes`);
      return res.data?.data ?? [];
    },
  });
  const classUtilization = (Array.isArray(classes) ? classes : []).map((c: { name: string; capacity: number; bookedCount: number }) => ({
    name: c.name,
    utilization: c.capacity > 0 ? Math.round((c.bookedCount / c.capacity) * 100) : 0,
  }));
  const memberGrowth = (analytics?.memberGrowth as { month: string; active: number; new: number }[] | undefined) ?? [];
  const { data: peakHoursRaw } = useQuery({
    queryKey: ['peak-hours', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const now = new Date();
      const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      const to = now.toISOString();
      const tzOffset = -now.getTimezoneOffset();
      const res = await apiClient.get(`/gyms/${gymId}/analytics/peak-hours`, {
        params: { from, to, tzOffsetMinutes: tzOffset },
      });
      return res.data.data ?? null;
    },
  });
  // Transform PeakHoursAnalyticsResponse { buckets: [{dayOfWeek,hour,count}] }
  // into rows { hour: "6:00 AM", mon: N, tue: N, ... } for the heatmap table.
  // C# DayOfWeek: 0=Sunday,1=Monday,...,6=Saturday
  type HeatRow = { hour: string; mon: number; tue: number; wed: number; thu: number; fri: number; sat: number; sun: number };
  const peakHours: HeatRow[] = (() => {
    const buckets = peakHoursRaw?.buckets;
    if (!Array.isArray(buckets) || buckets.length === 0) return [];
    const dayKeys: (keyof Omit<HeatRow, 'hour'>)[] = ['sun','mon','tue','wed','thu','fri','sat'];
    const matrix: Record<number, HeatRow> = {};
    for (const b of buckets) {
      if (!matrix[b.hour]) {
        const h = b.hour % 12 || 12;
        const ampm = b.hour < 12 ? 'AM' : 'PM';
        matrix[b.hour] = { hour: `${h}:00 ${ampm}`, mon:0, tue:0, wed:0, thu:0, fri:0, sat:0, sun:0 };
      }
      const key = dayKeys[b.dayOfWeek % 7];
      if (key) matrix[b.hour][key] = b.count;
    }
    return Object.keys(matrix).map(Number).sort((a,b) => a-b).map(h => matrix[h]!);
  })();

  return (
    <div className="space-y-6">
      <PageHeader title="Analytics" description="First Gym performance insights" />

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard value={formatNumber(analytics?.totalMembers ?? 0)} label="Active Members" icon={<Users className="h-5 w-5" />} variant="success" />
        <KpiCard value={formatCurrency(totalRevenue)} label="Revenue" icon={<DollarSign className="h-5 w-5" />} variant="info" />
        <KpiCard value={formatNumber(analytics?.checkInsToday ?? 0)} label="Daily Check-ins" icon={<DoorOpen className="h-5 w-5" />} />
        <KpiCard value={formatNumber(analytics?.totalClasses ?? 0)} label="Total Classes" icon={<BarChart3 className="h-5 w-5" />} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
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

        <ChartContainer title="Revenue Trend" subtitle="Monthly revenue (EGP)">
          <AreaChart data={monthlyRevenue}>
            <defs>
              <linearGradient id="colorStaffRev" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#C41E3A" stopOpacity={0.2} />
                <stop offset="95%" stopColor="#C41E3A" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
            <XAxis dataKey="month" tick={{ fontSize: 12 }} stroke="#9CA3AF" />
            <YAxis tick={{ fontSize: 12 }} stroke="#9CA3AF" tickFormatter={(v: number) => `${(v / 1000).toFixed(0)}K`} />
            <Tooltip formatter={(value: number) => formatCurrency(value)} />
            <Area type="monotone" dataKey="revenue" stroke="#C41E3A" strokeWidth={2} fill="url(#colorStaffRev)" />
          </AreaChart>
        </ChartContainer>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Peak Hours */}
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
                    {[row.mon, row.tue, row.wed, row.thu, row.fri, row.sat, row.sun].map((val, i) => (
                      <td key={i} className="py-1 px-2">
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
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Class Utilization */}
        <ChartContainer title="Class Utilization" subtitle="Enrollment vs capacity">
          <BarChart data={classUtilization} layout="vertical">
            <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
            <XAxis type="number" tick={{ fontSize: 12 }} stroke="#9CA3AF" domain={[0, 100]} tickFormatter={(v: number) => `${v}%`} />
            <YAxis type="category" dataKey="name" tick={{ fontSize: 11 }} stroke="#9CA3AF" width={120} />
            <Tooltip formatter={(value: number) => `${value}%`} />
            <Bar dataKey="utilization" fill="#C41E3A" radius={[0, 4, 4, 0]} />
          </BarChart>
        </ChartContainer>
      </div>
    </div>
  );
}

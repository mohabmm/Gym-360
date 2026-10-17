import { ChartContainer, formatCurrency } from '@gym360/ui';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';

interface Props {
  data: { month: string; revenue: number }[];
  subtitle: string;
  gradientId: string;
}

/** Monthly revenue area chart (dashboard + analytics). */
export function RevenueTrendChart({ data, subtitle, gradientId }: Props) {
  return (
    <ChartContainer title="Revenue Trend" subtitle={subtitle}>
      <AreaChart data={data}>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#C41E3A" stopOpacity={0.2} />
            <stop offset="95%" stopColor="#C41E3A" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
        <XAxis dataKey="month" tick={{ fontSize: 12 }} stroke="#9CA3AF" />
        <YAxis tick={{ fontSize: 12 }} stroke="#9CA3AF" tickFormatter={(v: number) => `${(v / 1000).toFixed(0)}K`} />
        <Tooltip formatter={(value: number) => formatCurrency(value)} />
        <Area type="monotone" dataKey="revenue" stroke="#C41E3A" strokeWidth={2} fill={`url(#${gradientId})`} />
      </AreaChart>
    </ChartContainer>
  );
}

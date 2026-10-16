import { ChartContainer, formatCurrency } from '@gym360/ui';
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import { METHOD_COLORS, type MonthBucket, type NameValue } from '@/lib/gym-revenue';

const thousands = (v: number) => `${(v / 1000).toFixed(0)}k`;
const currencyTooltip = (v: number) => formatCurrency(v);
const NO_DATA: NameValue[] = [{ name: 'No data', value: 1 }];

export function RevenueTrendChart({ trend, totalRevenue }: { trend: MonthBucket[]; totalRevenue: number }) {
  return (
    <ChartContainer
      title="Revenue Trend"
      subtitle={`Monthly completed revenue (${totalRevenue > 0 ? 'EGP' : 'no data'})`}
    >
      <AreaChart data={trend}>
        <defs>
          <linearGradient id="revGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#10B981" stopOpacity={0.4} />
            <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.2)" />
        <XAxis dataKey="label" tick={{ fontSize: 11 }} />
        <YAxis tick={{ fontSize: 11 }} tickFormatter={thousands} />
        <Tooltip formatter={currencyTooltip} contentStyle={{ borderRadius: 8, fontSize: 12 }} />
        <Area type="monotone" dataKey="revenue" stroke="#10B981" strokeWidth={2} fill="url(#revGrad)" />
      </AreaChart>
    </ChartContainer>
  );
}

function methodColor(name: string, hasData: boolean) {
  return hasData ? METHOD_COLORS[name] ?? '#94A3B8' : '#CBD5E1';
}

export function RevenueByMethodChart({ byMethod }: { byMethod: NameValue[] }) {
  const hasData = byMethod.length > 0;
  const data = hasData ? byMethod : NO_DATA;
  return (
    <ChartContainer title="By Method" subtitle="Revenue split by payment method" height={300}>
      <PieChart>
        <Pie
          data={data}
          dataKey="value"
          nameKey="name"
          cx="50%"
          cy="50%"
          outerRadius={90}
          label={(entry) => entry.name.replace(/_/g, ' ')}
        >
          {data.map((entry) => (
            <Cell key={entry.name} fill={methodColor(entry.name, hasData)} />
          ))}
        </Pie>
        <Tooltip formatter={currencyTooltip} />
      </PieChart>
    </ChartContainer>
  );
}

export function RevenueByPurposeChart({ byPurpose }: { byPurpose: NameValue[] }) {
  return (
    <ChartContainer title="By Purpose" subtitle="Revenue grouped by payment purpose" height={260}>
      <BarChart data={byPurpose} layout="vertical" margin={{ left: 60 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.2)" />
        <XAxis type="number" tick={{ fontSize: 11 }} tickFormatter={thousands} />
        <YAxis
          dataKey="name"
          type="category"
          tick={{ fontSize: 11 }}
          tickFormatter={(v: string) => v.replace(/_/g, ' ')}
          width={120}
        />
        <Tooltip formatter={currencyTooltip} />
        <Bar dataKey="value" fill="#8B5CF6" radius={[0, 4, 4, 0]} />
      </BarChart>
    </ChartContainer>
  );
}

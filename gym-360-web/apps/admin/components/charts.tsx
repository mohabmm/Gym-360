import { ChartContainer } from '@gym360/ui';
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';

// Charts are direct children of ChartContainer's ResponsiveContainer (it sizes
// its child by cloning it), so these are whole cards rather than bare charts.

const AXIS = { tick: { fontSize: 12 }, stroke: '#94A3B8' };
const GRID_STROKE = '#E2E8F0';

/** Formats an axis value in thousands, e.g. 12000 → "12K". */
export const thousands = (v: number) => `${(v / 1000).toFixed(0)}K`;

interface CardProps<T> {
  title: string;
  subtitle: string;
  data: T[];
}

interface AreaTrendCardProps<T> extends CardProps<T> {
  /** Unique per page: the SVG gradient is referenced by id. */
  gradientId: string;
  yTickFormatter?: (v: number) => string;
  valueFormatter?: (v: number) => string;
}

/** Green filled area chart of `value` over `date`. */
export function AreaTrendCard<T>({ title, subtitle, data, gradientId, yTickFormatter, valueFormatter }: AreaTrendCardProps<T>) {
  return (
    <ChartContainer title={title} subtitle={subtitle}>
      <AreaChart data={data}>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#10B981" stopOpacity={0.3} />
            <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
        <XAxis dataKey="date" {...AXIS} />
        <YAxis {...AXIS} tickFormatter={yTickFormatter} />
        {/* Recharts passes (value, name, …) — hand the formatter only the value. */}
        <Tooltip formatter={valueFormatter && ((value: number) => valueFormatter(value))} />
        <Area type="monotone" dataKey="value" stroke="#10B981" strokeWidth={2} fill={`url(#${gradientId})`} />
      </AreaChart>
    </ChartContainer>
  );
}

/** Vertical bar chart of `value` over `date`. */
export function BarTrendCard<T>({ title, subtitle, data, color }: CardProps<T> & { color: string }) {
  return (
    <ChartContainer title={title} subtitle={subtitle}>
      <BarChart data={data}>
        <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
        <XAxis dataKey="date" {...AXIS} />
        <YAxis {...AXIS} />
        <Tooltip />
        <Bar dataKey="value" fill={color} radius={[4, 4, 0, 0]} />
      </BarChart>
    </ChartContainer>
  );
}

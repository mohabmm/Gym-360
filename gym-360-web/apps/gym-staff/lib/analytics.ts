// C# DayOfWeek: 0=Sunday,1=Monday,...,6=Saturday
const DAY_KEYS = ['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'] as const;
type DayKey = (typeof DAY_KEYS)[number];

export type HeatRow = { hour: string } & Record<DayKey, number>;

export interface PeakHourBucket {
  dayOfWeek: number;
  hour: number;
  count: number;
}

function hourLabel(hour: number) {
  const h = hour % 12 || 12;
  const ampm = hour < 12 ? 'AM' : 'PM';
  return `${h}:00 ${ampm}`;
}

const emptyRow = (hour: number): HeatRow =>
  ({ hour: hourLabel(hour), mon: 0, tue: 0, wed: 0, thu: 0, fri: 0, sat: 0, sun: 0 });

/**
 * Transform PeakHoursAnalyticsResponse { buckets: [{dayOfWeek,hour,count}] }
 * into rows { hour: "6:00 AM", mon: N, tue: N, ... } for the heatmap table.
 */
export function toHeatRows(buckets: unknown): HeatRow[] {
  if (!Array.isArray(buckets) || buckets.length === 0) return [];
  const matrix: Record<number, HeatRow> = {};
  for (const b of buckets as PeakHourBucket[]) {
    matrix[b.hour] ??= emptyRow(b.hour);
    const key = DAY_KEYS[b.dayOfWeek % 7];
    if (key) matrix[b.hour][key] = b.count;
  }
  return Object.keys(matrix).map(Number).sort((a, b) => a - b).map((h) => matrix[h]!);
}

export function toClassUtilization(classes: unknown) {
  const list = Array.isArray(classes) ? classes : [];
  return list.map((c: { name: string; capacity: number; bookedCount: number }) => ({
    name: c.name,
    utilization: c.capacity > 0 ? Math.round((c.bookedCount / c.capacity) * 100) : 0,
  }));
}

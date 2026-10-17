export type RevenueRange = '6m' | '12m' | 'ytd';

export type PaymentRow = {
  id: string;
  memberId: string;
  memberName?: string | null;
  amount: number;
  currency: string;
  paymentMethod: string;
  purpose: string;
  status: string;
  receiptNumber: string;
  voidReason?: string | null;
  paidAt?: string | null;
  createdAt: string;
};

export type PagedPayments = { items: PaymentRow[]; page: number; pageSize: number; total: number };

export type MonthBucket = { key: string; label: string; revenue: number };

export type NameValue = { name: string; value: number };

export type RevenueTotals = {
  totalRevenue: number;
  voidedRevenue: number;
  voidedCount: number;
  thisMonth: number;
  prevMonth: number;
  momChange: number;
  completedCount: number;
};

export type RevenueSummary = {
  trend: MonthBucket[];
  totals: RevenueTotals;
  byMethod: NameValue[];
  byPurpose: NameValue[];
};

// BE sends enum names in PascalCase ("SessionPackage", "BankTransfer"); space them for display.
export const humanize = (v?: string | null): string =>
  String(v ?? '')
    .replace(/_/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2');

export const METHOD_COLORS: Record<string, string> = {
  cash: '#10B981',
  card: '#3B82F6',
  vodafone_cash: '#EF4444',
  instapay: '#8B5CF6',
  fawry: '#F59E0B',
  bank_transfer: '#6366F1',
};

function toDateOnly(d: Date) {
  return d.toISOString().slice(0, 10);
}

function firstOfMonth(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), 1);
}

function subMonths(d: Date, n: number) {
  return new Date(d.getFullYear(), d.getMonth() - n, 1);
}

function labelForMonth(d: Date) {
  return d.toLocaleDateString(undefined, { month: 'short' });
}

function monthKey(d: Date) {
  return `${d.getFullYear()}-${d.getMonth()}`;
}

const RANGE_MONTHS: Record<'6m' | '12m', number> = { '6m': 6, '12m': 12 };

function bucketCountFor(range: RevenueRange, now: Date) {
  return range === 'ytd' ? now.getMonth() + 1 : RANGE_MONTHS[range];
}

export function rangeBounds(range: RevenueRange, now = new Date()) {
  const end = toDateOnly(new Date(now.getFullYear(), now.getMonth() + 1, 0));
  const start =
    range === 'ytd'
      ? new Date(now.getFullYear(), 0, 1)
      : subMonths(firstOfMonth(now), RANGE_MONTHS[range] - 1);
  return { from: toDateOnly(start), to: end };
}

/** The list endpoint may return either a bare array or a paged envelope. */
export function unwrapPayments(raw: unknown): PaymentRow[] {
  if (Array.isArray(raw)) return raw as PaymentRow[];
  return (raw as PagedPayments)?.items ?? [];
}

export function paymentDate(p: PaymentRow) {
  return p.paidAt ?? p.createdAt;
}

const isStatus = (p: PaymentRow, status: string) => p.status.toLowerCase() === status;

function addTo(map: Map<string, number>, key: string, amount: number) {
  map.set(key, (map.get(key) ?? 0) + amount);
}

function toSortedList(map: Map<string, number>): NameValue[] {
  return Array.from(map, ([name, value]) => ({ name, value })).sort((a, b) => b.value - a.value);
}

function buildBuckets(range: RevenueRange, now: Date): MonthBucket[] {
  const bucketCount = bucketCountFor(range, now);
  return Array.from({ length: bucketCount }).map((_, i) => {
    const d = subMonths(firstOfMonth(now), bucketCount - 1 - i);
    return { key: monthKey(d), label: labelForMonth(d), revenue: 0 };
  });
}

function bucketRevenue(buckets: MonthBucket[], key: string) {
  return buckets.find((b) => b.key === key)?.revenue ?? 0;
}

function percentChange(current: number, previous: number) {
  return previous > 0 ? ((current - previous) / previous) * 100 : 0;
}

export function summarizeRevenue(payments: PaymentRow[], range: RevenueRange): RevenueSummary {
  const now = new Date();
  const buckets = buildBuckets(range, now);
  const bucketMap = new Map(buckets.map((b) => [b.key, b]));

  let completed = 0;
  let voided = 0;
  let voidedCount = 0;
  const methodTotals = new Map<string, number>();
  const purposeTotals = new Map<string, number>();

  for (const p of payments) {
    if (isStatus(p, 'completed')) {
      completed += p.amount;
      const bucket = bucketMap.get(monthKey(new Date(paymentDate(p))));
      if (bucket) bucket.revenue += p.amount;
      addTo(methodTotals, p.paymentMethod, p.amount);
      addTo(purposeTotals, p.purpose, p.amount);
    } else if (isStatus(p, 'voided')) {
      voided += p.amount;
      voidedCount++;
    }
  }

  const thisMonth = bucketRevenue(buckets, monthKey(now));
  const prevMonth = bucketRevenue(buckets, monthKey(subMonths(now, 1)));

  return {
    trend: buckets,
    totals: {
      totalRevenue: completed,
      voidedRevenue: voided,
      voidedCount,
      thisMonth,
      prevMonth,
      momChange: percentChange(thisMonth, prevMonth),
      completedCount: payments.filter((p) => isStatus(p, 'completed')).length,
    },
    byMethod: toSortedList(methodTotals),
    byPurpose: toSortedList(purposeTotals),
  };
}

export function latestPayments(payments: PaymentRow[], count: number) {
  return [...payments]
    .sort((a, b) => new Date(paymentDate(b)).getTime() - new Date(paymentDate(a)).getTime())
    .slice(0, count);
}

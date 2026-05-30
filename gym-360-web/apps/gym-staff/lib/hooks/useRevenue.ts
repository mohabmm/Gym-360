import { useQuery } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';
import type { PaymentListItem } from '@gym360/ui';

export interface RevenueSummary {
  monthlyRevenue: { month: string; revenue: number }[];
  recentPayments: PaymentListItem[];
}

export function useRevenue() {
  return useQuery<RevenueSummary>({
    queryKey: ['revenue'],
    queryFn: async () => {
      const [revenueRes, paymentsRes] = await Promise.all([
        apiClient.get('/billing/revenue'),
        apiClient.get('/billing/payments', { params: { page: 1, pageSize: 10 } }),
      ]);
      // /billing/revenue returns a summary object ({ totalRevenue, currency, … }) with
      // no monthlyBreakdown array. Only treat the payload as the series when it is
      // actually an array — otherwise the downstream `.reduce` blows up on the object.
      const rev = revenueRes.data.data;
      const monthlyRevenue = Array.isArray(rev?.monthlyBreakdown)
        ? rev.monthlyBreakdown
        : Array.isArray(rev)
          ? rev
          : [];
      const payments = paymentsRes.data.data;
      const recentPayments = Array.isArray(payments?.items)
        ? payments.items
        : Array.isArray(payments)
          ? payments
          : [];
      return { monthlyRevenue, recentPayments };
    },
  });
}

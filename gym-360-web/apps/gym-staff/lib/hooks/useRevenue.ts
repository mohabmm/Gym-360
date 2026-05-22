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
      const monthlyRevenue = revenueRes.data.data?.monthlyBreakdown ?? revenueRes.data.data ?? [];
      const recentPayments = paymentsRes.data.data?.items ?? paymentsRes.data.data ?? [];
      return { monthlyRevenue, recentPayments };
    },
  });
}

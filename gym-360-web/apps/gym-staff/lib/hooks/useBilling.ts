import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { PaymentListItem } from '@gym360/ui';

export interface MembershipPlan {
  id: string; name: string; description: string; durationDays: number;
  price: number; isActive: boolean; memberCount?: number;
}

export interface SessionPackageTemplate {
  id: string; name: string; sessionCount: number; pricePerSession: number;
  totalPrice: number; validityDays: number; isActive: boolean;
}

export interface ActivePackage {
  id: string; memberName: string; templateName: string;
  remaining: number; total: number; expiresAt: string; assignedBy?: string;
}

export function useMembershipPlans() {
  return useQuery<MembershipPlan[]>({
    queryKey: ['billing-plans'],
    queryFn: async () => {
      const res = await apiClient.get('/billing/plans', { params: { page: 1, pageSize: 50 } });
      return res.data.data?.items ?? res.data.data ?? [];
    },
  });
}

export function useSessionPackageTemplates() {
  return useQuery<SessionPackageTemplate[]>({
    queryKey: ['package-templates'],
    queryFn: async () => {
      const res = await apiClient.get('/billing/package-templates', { params: { page: 1, pageSize: 50 } });
      return res.data.data?.items ?? res.data.data ?? [];
    },
  });
}

export function useActivePackages() {
  return useQuery<ActivePackage[]>({
    queryKey: ['active-packages'],
    queryFn: async () => {
      const res = await apiClient.get('/billing/packages', { params: { page: 1, pageSize: 50 } });
      return res.data.data?.items ?? res.data.data ?? [];
    },
  });
}

// BE returns enum names in PascalCase (e.g. "Completed", "SessionPackage", "BankTransfer")
// and uses purpose/paymentMethod field names. The UI compares lowercase tokens and renders
// snake_case as spaced words, so normalize the shape here.
const toToken = (v: unknown): string =>
  String(v ?? '')
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .toLowerCase();

type RawPayment = {
  id: string;
  memberId: string;
  memberName?: string | null;
  amount: number;
  status?: string;
  paymentMethod?: string;
  method?: string;
  purpose?: string;
  type?: string;
  paidAt?: string | null;
  createdAt: string;
};

export function usePayments(pageSize = 50) {
  return useQuery<PaymentListItem[]>({
    queryKey: ['payments', pageSize],
    queryFn: async () => {
      const res = await apiClient.get('/billing/payments', { params: { page: 1, pageSize } });
      const raw: RawPayment[] = res.data.data?.items ?? res.data.data ?? [];
      return raw.map((p) => ({
        id: p.id,
        memberName: p.memberName ?? '—',
        amount: p.amount,
        status: toToken(p.status) as PaymentListItem['status'],
        method: toToken(p.paymentMethod ?? p.method) as PaymentListItem['method'],
        type: toToken(p.purpose ?? p.type) as PaymentListItem['type'],
        createdAt: p.paidAt ?? p.createdAt,
      }));
    },
  });
}

export function useCreatePayment() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const userId = session?.user?.id;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      memberId: string; amount: number; method: string; type: string; description?: string;
    }) => {
      const res = await apiClient.post('/billing/payments', {
        gymId,
        memberId: data.memberId,
        amount: data.amount,
        paymentMethod: data.method,
        purpose: data.type,
        recordedBy: userId,
      });
      return res.data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['payments'] });
      qc.invalidateQueries({ queryKey: ['revenue'] });
    },
  });
}

export function useCreateMembershipPlan() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: { name: string; durationDays: number; price: number; features?: string[] }) => {
      const res = await apiClient.post('/billing/plans', {
        gymId,
        name: data.name,
        durationDays: data.durationDays,
        price: data.price,
        features: data.features ?? [],
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['billing-plans'] }),
  });
}

export function useUpdateMembershipPlan() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: {
      id: string; name?: string; durationDays?: number; price?: number; isActive?: boolean;
    }) => {
      const res = await apiClient.put(`/billing/plans/${id}`, data);
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['billing-plans'] }),
  });
}

export function useCreatePackageTemplate() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: { name: string; sessionCount: number; pricePerSession: number }) => {
      const res = await apiClient.post('/billing/package-templates', {
        gymId,
        name: data.name,
        sessionCount: data.sessionCount,
        pricePerSession: data.pricePerSession,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['package-templates'] }),
  });
}

export function useVoidPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      const res = await apiClient.post(`/billing/payments/${id}/void`, { reason });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payments'] }),
  });
}

export function useUpdatePackageTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: {
      id: string; name?: string; sessionCount?: number; pricePerSession?: number; isActive?: boolean;
    }) => {
      const res = await apiClient.put(`/billing/package-templates/${id}`, data);
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['package-templates'] }),
  });
}

export function useAssignPackage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: { memberId: string; templateId: string }) => {
      const res = await apiClient.post('/billing/packages/assign', {
        memberId: data.memberId,
        templateId: data.templateId,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['active-packages'] }),
  });
}

export interface MySubscription {
  id: string;
  gymId: string;
  planId: string;
  planCode: string;
  planName: string;
  startedAt: string;
  expiresAt: string;
  isActive: boolean;
  isTrial: boolean;
  trialExpiresAt: string | null;
  paymentStatus: string;
  lastPaymentAt: string | null;
  nextPaymentDue: string | null;
  overdueSince: string | null;
  gracePeriodEndsAt: string | null;
}

export function useMySubscription() {
  return useQuery<MySubscription | null>({
    queryKey: ['my-subscription'],
    queryFn: async () => {
      const res = await apiClient.get('/billing/my-subscription');
      return res.data.data ?? null;
    },
  });
}

export interface PlanFeatureSet {
  planCode: string;
  enabledFeatures: string[];
  memberLimit: number;
  trainerLimit: number;
  staffLimit: number;
}

export function useMyPlanFeatures() {
  return useQuery<PlanFeatureSet | null>({
    queryKey: ['my-plan-features'],
    queryFn: async () => {
      const res = await apiClient.get('/billing/plan-features');
      return res.data.data ?? null;
    },
  });
}

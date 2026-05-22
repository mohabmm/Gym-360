export type PaymentStatus = 'pending' | 'completed' | 'failed' | 'voided' | 'refunded';

export type PaymentMethod = 'cash' | 'card' | 'bank_transfer' | 'fawry' | 'vodafone_cash' | 'instapay';

export type PaymentType = 'membership' | 'session_package' | 'personal_training' | 'class' | 'other';

export interface Payment {
  id: string;
  memberId: string;
  memberName: string;
  gymId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  method: PaymentMethod;
  type: PaymentType;
  description?: string;
  receiptNumber?: string;
  voidedAt?: string;
  voidReason?: string;
  createdAt: string;
  processedBy?: string;
}

export interface PaymentListItem {
  id: string;
  memberName: string;
  amount: number;
  status: PaymentStatus;
  method: PaymentMethod;
  type: PaymentType;
  createdAt: string;
}

export interface RevenueBreakdown {
  total: number;
  memberships: number;
  sessionPackages: number;
  personalTraining: number;
  classes: number;
  other: number;
}

export interface RevenueTrend {
  date: string;
  revenue: number;
  membershipRevenue: number;
  trainingRevenue: number;
}

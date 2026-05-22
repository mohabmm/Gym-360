export type GymStatus = 'active' | 'suspended' | 'pending' | 'terminated' | 'grace_period';

export type GymSection = 'Mens' | 'Womens' | 'Mixed';

export interface Address {
  street: string;
  city: string;
  district: string;
  governorate: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
}

export interface GymBranding {
  logoUrl?: string;
  primaryColor: string;
  secondaryColor: string;
  coverImageUrl?: string;
}

export interface OperatingHours {
  dayOfWeek: number;
  openTime: string;
  closeTime: string;
  isClosed: boolean;
}

export interface SeasonalHours {
  id: string;
  name: string;
  nameAr: string;
  startDate: string;
  endDate: string;
  hours: OperatingHours[];
}

export interface GymSessionPolicy {
  cancellationWindowHours: number;
  noShowPenaltyAction: 'deduct_session' | 'warn' | 'none';
  lateCancelDeductsSession: boolean;
}

export interface Gym {
  id: string;
  name: string;
  nameAr: string;
  gymCode: string;
  status: GymStatus;
  address: Address;
  branding: GymBranding;
  phone: string;
  email: string;
  sections: GymSection[];
  activeMemberCount: number;
  activeTrainerCount: number;
  monthlyRevenue?: number;
  operatingHours: OperatingHours[];
  seasonalHours: SeasonalHours[];
  sessionPolicy: GymSessionPolicy;
  planId: string;
  planName: string;
  subscriptionStatus: string;
  paymentStatus: PaymentAlertStatus;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  amenities: string[];
  socialMedia: GymSocialMedia;
}

export interface GymSocialMedia {
  facebook?: string;
  instagram?: string;
  twitter?: string;
  tiktok?: string;
  website?: string;
}

export type PaymentAlertStatus = 'current' | 'overdue' | 'warning' | 'none';

export interface GymListItem {
  id: string;
  name: string;
  nameAr: string;
  gymCode: string;
  status: GymStatus;
  city: string;
  planName: string;
  activeMemberCount: number;
  activeTrainerCount: number;
  paymentStatus: PaymentAlertStatus;
  createdAt: string;
  ownerName: string;
  monthlyRevenue: number;
}

export interface PendingGym {
  id: string;
  name: string;
  ownerName: string;
  ownerEmail: string;
  ownerPhone: string;
  city: string;
  district: string;
  requestedPlan: string;
  submittedAt: string;
  notes?: string;
}

export interface GymAmenity {
  id: string;
  name: string;
  nameAr: string;
  icon: string;
}

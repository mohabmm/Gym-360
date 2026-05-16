export type TrainerStatus = 'active' | 'inactive' | 'pending_approval' | 'suspended';

export interface Trainer {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  firstNameAr: string;
  lastNameAr: string;
  email: string;
  phone: string;
  avatarUrl?: string;
  status: TrainerStatus;
  specializations: string[];
  certifications: string[];
  bio?: string;
  bioAr?: string;
  activeMemberCount: number;
  rating: number;
  totalRatings: number;
  monthlyScore: number;
  joinDate: string;
  gymIds: string[];
  isRemoteEnabled: boolean;
  remoteRate?: number;
  inPersonRate?: number;
}

export interface TrainerListItem {
  id: string;
  name: string;
  nameAr: string;
  email?: string;
  phone?: string;
  avatarUrl?: string;
  status: TrainerStatus;
  specializations: string[];
  bio?: string;
  activeMemberCount: number;
  rating: number;
  monthlyScore: number;
}

export interface TrainerAvailability {
  id: string;
  trainerId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
}

export interface TrainerRating {
  id: string;
  trainerId: string;
  memberId: string;
  memberName: string;
  rating: number;
  comment?: string;
  createdAt: string;
}

export interface TrainerMonthlyScore {
  month: string;
  score: number;
  sessionsCompleted: number;
  memberSatisfaction: number;
  attendance: number;
}

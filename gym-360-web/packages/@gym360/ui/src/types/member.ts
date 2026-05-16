export type MemberStatus = 'active' | 'inactive' | 'frozen' | 'expired';

export type MembershipType = 'monthly' | 'quarterly' | 'semi_annual' | 'annual' | 'session_package';

export interface Member {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  firstNameAr: string;
  lastNameAr: string;
  email: string;
  phone: string;
  avatarUrl?: string;
  status: MemberStatus;
  membershipType: MembershipType;
  joinDate: string;
  expiryDate: string;
  gymId: string;
  section: string;
  emergencyContact?: string;
  emergencyPhone?: string;
  notes?: string;
  totalPayments: number;
  lastCheckIn?: string;
  checkInCount: number;
  assignedTrainerId?: string;
  assignedTrainerName?: string;
}

export interface MemberListItem {
  id: string;
  name: string;
  nameAr: string;
  email: string;
  phone: string;
  status: MemberStatus;
  membershipType: MembershipType;
  joinDate: string;
  expiryDate: string;
  section: string;
  lastCheckIn?: string;
  avatarUrl?: string;
}

export interface MemberCheckIn {
  id: string;
  memberId: string;
  memberName: string;
  checkInTime: string;
  checkOutTime?: string;
  method: 'qr' | 'manual' | 'numpad';
  section: string;
}

export interface MemberProgress {
  date: string;
  weight?: number;
  bodyFatPercentage?: number;
  muscleMass?: number;
  bmi?: number;
}

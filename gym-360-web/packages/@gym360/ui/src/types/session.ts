export type SessionStatus = 'scheduled' | 'in_progress' | 'completed' | 'cancelled' | 'no_show' | 'late_cancel';

/**
 * Session type strings produced by the backend SessionType enum with camelCase conversion.
 * The string values match the .NET enum names (see Scheduling.Domain.Enums.SessionType).
 */
export type SessionType = string;

/**
 * Matches the backend SessionDto returned by Scheduling query endpoints.
 * See docs/01_GYMAPP_BACKEND_COMPLETE.md MODULE 5 and
 * src/Modules/Scheduling/GymApp.Modules.Scheduling.Application/DTOs/SessionDto.cs.
 *
 * Display-only fields (trainerName, memberName, workoutName, notes) are NOT in the
 * envelope — fetch them separately via identity/training endpoints when needed.
 */
export interface Session {
  id: string;
  gymId: string;
  trainerId: string;
  memberId: string;
  scheduledAt: string;
  durationMinutes: number;
  sessionType: SessionType;
  focus?: string;
  status: SessionStatus;
  isRecurring: boolean;
  createdAt: string;
}

export interface GymClass {
  id: string;
  name: string;
  nameAr: string;
  description?: string;
  trainerId: string;
  trainerName: string;
  gymId: string;
  section: string;
  capacity: number;
  enrolledCount: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
  status: 'active' | 'cancelled' | 'full';
  color?: string;
}

export interface ClassEnrollment {
  id: string;
  classId: string;
  memberId: string;
  memberName: string;
  enrolledAt: string;
  status: 'enrolled' | 'attended' | 'absent' | 'cancelled';
}

export interface SessionPackage {
  id: string;
  memberId: string;
  templateId: string;
  templateName: string;
  totalSessions: number;
  usedSessions: number;
  remainingSessions: number;
  expiryDate: string;
  status: 'active' | 'expired' | 'depleted';
  purchasedAt: string;
}

export interface SessionPackageTemplate {
  id: string;
  name: string;
  nameAr: string;
  sessionCount: number;
  validityDays: number;
  price: number;
  gymId: string;
  isActive: boolean;
}

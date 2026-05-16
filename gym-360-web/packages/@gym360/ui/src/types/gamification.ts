export interface LeaderboardEntry {
  rank: number;
  memberId: string;
  memberName: string;
  memberNameAr: string;
  avatarUrl?: string;
  score: number;
  workoutsCompleted: number;
  streakDays: number;
  change: number;
}

export interface TrainerRankingEntry {
  rank: number;
  trainerId: string;
  trainerName: string;
  trainerNameAr: string;
  avatarUrl?: string;
  monthlyScore: number;
  rating: number;
  totalRatings: number;
  sessionsCompleted: number;
  memberSatisfaction: number;
}

export interface Testimonial {
  id: string;
  memberId: string;
  memberName: string;
  trainerId: string;
  trainerName: string;
  content: string;
  rating: number;
  status: 'pending' | 'approved' | 'rejected';
  createdAt: string;
  moderatedAt?: string;
  moderatedBy?: string;
}

export interface LeaderboardOptOut {
  memberId: string;
  optedOutAt: string;
  reason?: string;
}

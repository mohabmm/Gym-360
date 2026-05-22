export type NotificationType =
  | 'announcement'
  | 'payment_reminder'
  | 'session_reminder'
  | 'class_update'
  | 'membership_expiry'
  | 'system'
  | 'achievement'
  | 'config_update';

export type NotificationChannel = 'push' | 'in_app' | 'email' | 'sms';

export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  titleAr: string;
  body: string;
  bodyAr: string;
  channel: NotificationChannel;
  isRead: boolean;
  data?: Record<string, string>;
  createdAt: string;
  readAt?: string;
}

export interface Announcement {
  id: string;
  gymId: string;
  title: string;
  titleAr: string;
  body: string;
  bodyAr: string;
  targetAudience: 'all' | 'members' | 'trainers' | 'staff';
  targetSection?: string;
  isActive: boolean;
  publishedAt: string;
  expiresAt?: string;
  createdBy: string;
  createdByName: string;
}

export interface NotificationPreference {
  type: NotificationType;
  push: boolean;
  email: boolean;
  sms: boolean;
  inApp: boolean;
}

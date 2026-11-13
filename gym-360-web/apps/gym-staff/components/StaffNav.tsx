import type { ReactNode } from 'react';
import { isPathEnabled, type NavSection, type StaffSubRole } from '@gym360/ui';
import {
  LayoutDashboard, Users, Dumbbell, CalendarDays, BookOpen,
  CreditCard, Trophy, Megaphone, BarChart3, DoorOpen, UserCog, Settings,
  FileSpreadsheet, Lock, Gift, AlertTriangle, LineChart, ThumbsUp,
} from 'lucide-react';
import { STAFF_ROUTE_FLAGS } from '@/lib/feature-flags';
import type { PtText } from '@/lib/pt-text';

const icon = (Icon: typeof Users): ReactNode => <Icon className="h-5 w-5" />;

function allSections(t: PtText, subRole: StaffSubRole): NavSection[] {
  const canSeePtDashboard = subRole === 'owner' || subRole === 'manager';
  return [
    {
      label: 'Main',
      items: [
        { label: 'Dashboard', href: '/', icon: icon(LayoutDashboard) },
        { label: 'Members', href: '/members', icon: icon(Users) },
        { label: 'Trainers', href: '/trainers', icon: icon(Dumbbell) },
        { label: 'Classes', href: '/classes', icon: icon(CalendarDays) },
        { label: 'Exercises', href: '/exercises', icon: icon(BookOpen) },
      ],
    },
    {
      label: t.navPt,
      items: [
        { label: t.navInterests, href: '/pt/interests', icon: icon(ThumbsUp) },
        { label: t.navTrials, href: '/pt/trials', icon: icon(Gift) },
        { label: t.navReview, href: '/pt/review', icon: icon(AlertTriangle) },
        ...(canSeePtDashboard ? [{ label: t.navDashboard, href: '/pt/dashboard', icon: icon(LineChart) }] : []),
      ],
    },
    {
      label: 'Management',
      items: [
        { label: 'Messages', href: '/messages', icon: icon(Megaphone) },
        { label: 'Billing', href: '/billing', icon: icon(CreditCard) },
        { label: 'My Subscription', href: '/subscription', icon: icon(CreditCard) },
        { label: 'Gamification', href: '/gamification', icon: icon(Trophy) },
        { label: 'Announcements', href: '/announcements', icon: icon(Megaphone) },
        { label: 'Analytics', href: '/analytics', icon: icon(BarChart3) },
        { label: 'Check-Ins', href: '/check-ins', icon: icon(DoorOpen) },
      ],
    },
    {
      label: 'Settings',
      items: [
        { label: 'Staff', href: '/staff', icon: icon(UserCog) },
        { label: 'Data Import', href: '/data-import', icon: icon(FileSpreadsheet) },
        { label: 'Kiosk PIN', href: '/kiosk-pin', icon: icon(Lock) },
        { label: 'Settings', href: '/settings', icon: icon(Settings) },
      ],
    },
  ];
}

/** Sidebar sections with disabled features removed; sections left empty are dropped (no gaps). */
export function staffNavSections(t: PtText, subRole: StaffSubRole, isEnabled: (key: string) => boolean): NavSection[] {
  return allSections(t, subRole)
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => isPathEnabled(item.href, STAFF_ROUTE_FLAGS, isEnabled)),
    }))
    .filter((section) => section.items.length > 0);
}

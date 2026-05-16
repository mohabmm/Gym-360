'use client';

import { type ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { SidebarNav, type NavSection, LocaleToggle, ThemeToggle } from '@gym360/ui';
import Image from 'next/image';
import {
  LayoutDashboard, Building2, Users, CreditCard, Layers, BarChart3, TrendingDown,
  BookOpen, Settings, UserPlus, FileBarChart, HeadphonesIcon,
} from 'lucide-react';
import { usePendingGyms } from '@/lib/hooks';
import { signOut, useSession } from 'next-auth/react';

export default function DashboardLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const { data: pendingData } = usePendingGyms();
  const { data: session } = useSession();
  const pendingCount = pendingData?.data?.length ?? 0;

  const navSections: NavSection[] = [
    {
      label: 'Main',
      items: [
        { label: 'Dashboard', href: '/', icon: <LayoutDashboard className="h-4 w-4" /> },
        { label: 'Gyms', href: '/gyms', icon: <Building2 className="h-4 w-4" /> },
        { label: 'Gym Onboarding', href: '/gyms/onboarding', icon: <UserPlus className="h-4 w-4" />, badge: pendingCount || undefined },
        { label: 'Users', href: '/users', icon: <Users className="h-4 w-4" /> },
        { label: 'Subscriptions', href: '/subscriptions', icon: <CreditCard className="h-4 w-4" /> },
        { label: 'Plans & Features', href: '/plans', icon: <Layers className="h-4 w-4" /> },
      ],
    },
    {
      label: 'Analytics',
      items: [
        { label: 'Platform Analytics', href: '/analytics', icon: <BarChart3 className="h-4 w-4" /> },
        { label: 'Churn Analytics', href: '/analytics/churn', icon: <TrendingDown className="h-4 w-4" /> },
      ],
    },
    {
      label: 'Operations',
      items: [
        { label: 'Content', href: '/content', icon: <BookOpen className="h-4 w-4" /> },
        { label: 'Reports', href: '/reports', icon: <FileBarChart className="h-4 w-4" /> },
        { label: 'Support', href: '/support', icon: <HeadphonesIcon className="h-4 w-4" /> },
        { label: 'Settings', href: '/settings', icon: <Settings className="h-4 w-4" /> },
      ],
    },
  ];

  return (
    <div className="flex h-screen overflow-hidden">
      <SidebarNav
        sections={navSections}
        variant="admin"
        logo={
          <div className="flex items-center gap-2">
            <Image
              src="/logo.svg"
              alt="Gym 360"
              width={28}
              height={28}
              priority
              className="h-7 w-7 shrink-0"
            />
            <div className="flex items-center gap-2">
              <span className="font-heading font-bold text-white">Gym 360</span>
              <span className="text-[10px] font-medium bg-emerald-500/20 text-emerald-400 px-1.5 py-0.5 rounded">
                Admin
              </span>
            </div>
          </div>
        }
        logoCollapsed={
          <Image
            src="/logo.svg"
            alt="Gym 360"
            width={28}
            height={28}
            priority
            className="h-7 w-7"
          />
        }
        userName={session?.user?.name ?? 'Admin'}
        userRole={session?.user?.adminRole ?? 'admin'}
        onSignOut={() => {
          void signOut({ callbackUrl: '/login' });
        }}
      />
      <main className="flex-1 ml-[260px] overflow-y-auto">
        <div className="sticky top-0 z-20 flex items-center justify-end gap-2 border-b border-border bg-background/80 px-6 py-3 backdrop-blur">
          <LocaleToggle onAfterChange={() => router.refresh()} />
          <ThemeToggle />
        </div>
        <div className="p-6">{children}</div>
      </main>
    </div>
  );
}

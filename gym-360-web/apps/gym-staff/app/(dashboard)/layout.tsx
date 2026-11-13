'use client';

import { type ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { signOut, useSession } from 'next-auth/react';
import { SidebarNav, LocaleToggle, ThemeToggle, GymSwitcher, getFeatureFlagService, type GymOption } from '@gym360/ui';
import { staffNavSections } from '@/components/StaffNav';
import { FeatureRouteGuard } from '@/components/FeatureRouteGuard';
import { useStaffFlags } from '@/lib/feature-flags';
import { usePtText } from '@/lib/pt-text';

function logoWords(gymName: string): { top: string; bottom: string } {
  const words = (gymName || 'Gym').trim().split(/\s+/).filter(Boolean);
  const top = (words[0] ?? 'GYM').toUpperCase().slice(0, 6);
  const bottom = (words.slice(1).join(' ') || '').toUpperCase().slice(0, 6);
  return { top, bottom };
}

function logoInitials(gymName: string): string {
  const initials = (gymName || 'Gym')
    .split(/\s+/)
    .filter(Boolean)
    .map((w) => w[0] ?? '')
    .join('')
    .slice(0, 2)
    .toUpperCase();
  return initials || 'GY';
}

function GymLogo({ gymName }: { gymName: string }) {
  const { top, bottom } = logoWords(gymName);
  return (
    <div className="flex items-center gap-3">
      <div className="h-10 w-10 shrink-0 flex flex-col items-center justify-center rounded-xl bg-[#C41E3A] text-white font-heading font-extrabold leading-none">
        <span className="text-[10px] tracking-tight">{top}</span>
        {bottom && <span className="text-[8px] tracking-wide text-[#D4AF37]">{bottom}</span>}
      </div>
      <div>
        <div className="font-heading font-bold text-sm leading-tight">{gymName}</div>
        <p className="text-xs text-muted-foreground">Gym Staff Portal</p>
      </div>
    </div>
  );
}

function GymLogoCollapsed({ gymName }: { gymName: string }) {
  return (
    <div className="h-10 w-10 shrink-0 flex flex-col items-center justify-center rounded-xl bg-[#C41E3A] text-white font-heading font-extrabold leading-none">
      <span className="text-[10px] tracking-tight">{logoInitials(gymName)}</span>
    </div>
  );
}

export default function DashboardLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const { data: session, update } = useSession();

  const handleSignOut = async () => {
    getFeatureFlagService().clear();
    await signOut({ redirect: false });
    router.push('/login');
  };

  // Hard-mode multi-tenancy (FOUNDER_DECISIONS Q3 revised 2026-05-01).
  // Owners surface their owned gyms in session.user.ownedGyms; staff/managers
  // collapse to the single membership gym.
  const ownedGyms = session?.user?.ownedGyms ?? [];
  const activeGymId = session?.user?.gymId ?? '';
  const isOwner = session?.user?.role === 'gym_owner';

  const switcherGyms: GymOption[] = isOwner
    ? ownedGyms.map((g) => ({
        id: g.gymId,
        name: g.name,
        nameAr: g.name,
        role: 'Owner',
        city: '',
      }))
    : activeGymId
    ? [{ id: activeGymId, name: session?.user?.gymName ?? '', nameAr: session?.user?.gymName ?? '', role: 'Staff', city: '' }]
    : [];

  const handleSwitchGym = async (gymId: string) => {
    if (!isOwner) return;

    try {
      const res = await fetch('/api/select-gym', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ gymId }),
      });
      if (!res.ok) throw new Error('Switch failed');

      const { accessToken, gymId: newGymId, gymName, schemaPrefix } = await res.json();
      await update({ accessToken, gymId: newGymId, gymName, schemaPrefix });

      if (typeof document !== 'undefined') {
        document.cookie = `x-gym-id=${newGymId}; path=/; SameSite=Lax`;
      }
      router.refresh();
    } catch (e) {
      console.error('Failed to switch gym', e);
    }
  };

  const gymName = session?.user?.gymName || (switcherGyms[0]?.name ?? '');
  const userName = session?.user?.name ?? 'Staff';
  const staffSubRole = session?.user?.staffSubRole ?? 'owner';
  const { isEnabled } = useStaffFlags();
  const navSections = staffNavSections(usePtText(), staffSubRole, isEnabled);

  return (
    <div className="flex min-h-screen">
      <SidebarNav
        sections={navSections}
        variant="staff"
        logo={<GymLogo gymName={gymName} />}
        logoCollapsed={<GymLogoCollapsed gymName={gymName} />}
        userName={userName}
        userRole={staffSubRole}
        onSignOut={handleSignOut}
      />
      <main className="flex-1 ml-[260px] transition-all duration-300">
        <div className="sticky top-0 z-20 flex items-center justify-end gap-2 border-b border-border bg-background/80 px-6 py-3 backdrop-blur">
          {switcherGyms.length > 0 && (
            <GymSwitcher
              gyms={switcherGyms}
              currentGymId={activeGymId}
              onSwitch={handleSwitchGym}
              className="mr-2"
            />
          )}
          <LocaleToggle onAfterChange={() => router.refresh()} />
          <ThemeToggle />
        </div>
        <div className="p-6 max-w-[1400px] mx-auto">
          <FeatureRouteGuard>{children}</FeatureRouteGuard>
        </div>
      </main>
    </div>
  );
}
